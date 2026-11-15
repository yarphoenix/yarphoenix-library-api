using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Domain;

namespace YarPhoenix.Catalog.Infrastructure.Collection;

// Собирает «читаемую коллекцию»: работы Open Library с изданием в Standard Ebooks или Project
// Gutenberg, прошедшие фильтр качества и правило общественного достояния. Первый проход — при
// пустой коллекции, дальше раз в RefreshDays. Проход возобновляемый: страница сохраняется после
// каждого шага, а upsert по (Provider, ExternalId) идемпотентен.
public sealed class ReadableCollectionImporter(
    IServiceScopeFactory scopeFactory,
    IOptions<CollectionOptions> options,
    ILogger<ReadableCollectionImporter> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan IdleCheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(15);

    private readonly CollectionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.ImportEnabled)
        {
            logger.LogInformation("Readable collection import is disabled (Collection:ImportEnabled)");
            return;
        }

        try
        {
            // Старт приложения и первые запросы важнее фоновой работы.
            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var pause = IdleCheckInterval;
                try
                {
                    if (await IsDueAsync(stoppingToken))
                    {
                        await RunAsync(stoppingToken);
                    }
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    // Источник недоступен — хост не роняем: пройденные страницы сохранены, продолжим позже.
                    logger.LogWarning(
                        exception,
                        "Readable collection import was interrupted; next attempt in {Minutes} min",
                        RetryInterval.TotalMinutes);
                    pause = RetryInterval;
                }

                await Task.Delay(pause, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Остановка приложения.
        }
    }

    private async Task<bool> IsDueAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var state = await dbContext.CollectionImportStates
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == CollectionImportState.SingletonId, cancellationToken);

        // Ни одного полного прохода или прерванный проход — продолжаем сразу.
        if (state?.LastCompletedAtUtc is not { } completedAt || state.NextPage > 1)
        {
            return true;
        }

        return DateTime.UtcNow - completedAt >= TimeSpan.FromDays(_options.RefreshDays);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var catalog = await LoadGutenbergCatalogAsync(cancellationToken);
        var currentYear = DateTime.UtcNow.Year;
        var pageSize = Math.Clamp(_options.PageSize, 1, 1000);

        while (true)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var provider = scope.ServiceProvider.GetRequiredService<IBookMetadataProvider>();

            var state = await GetOrCreateStateAsync(dbContext, cancellationToken);
            var pageNumber = state.NextPage;
            var page = await provider.ListReadableWorksAsync(pageNumber, pageSize, cancellationToken);

            if (page.Works.Count == 0)
            {
                state.NextPage = 1;
                state.LastCompletedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);

                var total = await dbContext.Books.CountAsync(b => b.InCollection, cancellationToken);
                logger.LogInformation("Readable collection import completed: {Total} books in the collection", total);
                return;
            }

            var entries = page.Works
                .Select(work => CollectionEntryFactory.Create(work, catalog, currentYear))
                .OfType<CollectionEntry>()
                .DistinctBy(entry => entry.ExternalId)
                .ToList();

            await UpsertAsync(dbContext, entries, cancellationToken);
            state.NextPage = pageNumber + 1;
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Readable collection import: page {Page} of ~{Pages}, accepted {Accepted} of {Seen} works",
                pageNumber,
                (int)Math.Ceiling(page.Total / (double)pageSize),
                entries.Count,
                page.Works.Count);

            await Task.Delay(_options.RequestDelayMilliseconds, cancellationToken);
        }
    }

    private async Task<GutenbergCatalog> LoadGutenbergCatalogAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<IPublicDomainSource>();

        logger.LogInformation("Readable collection import: loading the Project Gutenberg catalog");
        await using var feed = await source.DownloadCatalogFeedAsync(cancellationToken);
        using var reader = new StreamReader(feed);
        var catalog = GutenbergCatalog.Parse(reader);

        logger.LogInformation("Project Gutenberg catalog loaded: {Count} text editions", catalog.Count);
        return catalog;
    }

    private static async Task<CollectionImportState> GetOrCreateStateAsync(
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var state = await dbContext.CollectionImportStates
            .SingleOrDefaultAsync(s => s.Id == CollectionImportState.SingletonId, cancellationToken);

        if (state is null)
        {
            state = new CollectionImportState();
            dbContext.CollectionImportStates.Add(state);
        }

        return state;
    }

    private async Task UpsertAsync(
        CatalogDbContext dbContext,
        IReadOnlyList<CollectionEntry> entries,
        CancellationToken cancellationToken)
    {
        var externalIds = entries.Select(entry => entry.ExternalId).ToList();
        var existing = await dbContext.Books
            .Where(b => b.Provider == BookProvider.OpenLibrary && externalIds.Contains(b.ExternalId!))
            .ToDictionaryAsync(b => b.ExternalId!, cancellationToken);

        foreach (var entry in entries)
        {
            if (!existing.TryGetValue(entry.ExternalId, out var book))
            {
                book = new Book
                {
                    Id = Guid.NewGuid(),
                    Provider = BookProvider.OpenLibrary,
                    ExternalId = entry.ExternalId,
                };
                dbContext.Books.Add(book);
            }

            await ApplyAsync(dbContext, book, entry, cancellationToken);
        }
    }

    private async Task ApplyAsync(
        CatalogDbContext dbContext,
        Book book,
        CollectionEntry entry,
        CancellationToken cancellationToken)
    {
        book.Title = entry.Title;
        book.Author = entry.Author;
        book.CoverId = entry.CoverId;
        book.FirstPublishYear = entry.FirstPublishYear;
        book.Subjects = entry.Subjects;
        book.Genres = entry.Genres;
        book.Popularity = entry.Popularity;
        book.Language = "en";
        book.InCollection = true;

        if (book.FileId is not null && book.ReadableSource == ReadableSource.None)
        {
            // Файл из прежнего источника (OCR-скан Internet Archive). Отвязываем его: «Читать»
            // скачает издание коллекции. Позиции чтения относились к старому файлу — сбрасываем.
            logger.LogInformation("Detaching the legacy file {FileId} from book {BookId}", book.FileId, book.Id);
            book.FileId = null;
            await dbContext.UserBooks
                .Where(ub => ub.BookId == book.Id)
                .ExecuteUpdateAsync(
                    update => update
                        .SetProperty(ub => ub.CfiPosition, (string?)null)
                        .SetProperty(ub => ub.Progress, 0d),
                    cancellationToken);
        }

        // Источник уже скачанного файла не переписываем: он описывает то, что лежит в MinIO.
        if (book.FileId is null)
        {
            book.ReadableSource = entry.Source;
            book.SourceRef = entry.SourceRef;
            book.FallbackGutenbergId = entry.FallbackGutenbergId;
        }
    }
}
