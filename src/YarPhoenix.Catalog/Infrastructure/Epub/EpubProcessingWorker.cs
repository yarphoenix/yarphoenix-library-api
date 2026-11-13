using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Domain;
using YarPhoenix.Storage;

namespace YarPhoenix.Catalog.Infrastructure.Epub;

// Фоновая цепочка «парсинг OPF → матчинг с каталогом» (ТЗ §4.3, §5.4).
// Логика матчинга принадлежит Catalog-модулю, исполняется здесь — не в запросе загрузки.
public sealed class EpubProcessingWorker(
    BookProcessingQueue queue,
    IFileStorage fileStorage,
    IServiceScopeFactory scopeFactory,
    ILogger<EpubProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var bookId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(bookId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Ошибка одной книги не должна останавливать очередь: книга просто
                // остаётся с метаданными из имени файла.
                logger.LogError(exception, "EPUB processing failed for book {BookId}", bookId);
            }
        }
    }

    private async Task ProcessAsync(Guid bookId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing uploaded book {BookId}", bookId);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var book = await dbContext.Books.SingleOrDefaultAsync(b => b.Id == bookId, cancellationToken);
        if (book?.FileId is null)
        {
            return;
        }

        // Свой таймаут на чтение из MinIO: зависший вызов не должен блокировать очередь.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        await using var content = await fileStorage.OpenBookAsync(book.FileId, timeout.Token);
        logger.LogInformation("Opened file {FileId} ({Bytes} bytes)", book.FileId, content.Length);

        var metadata = EpubMetadataParser.Parse(content);
        logger.LogInformation(
            "Parsed EPUB metadata for {BookId}: {Title} / {Author} / {Isbn}",
            bookId,
            metadata?.Title,
            metadata?.Author,
            metadata?.Isbn);

        if (metadata is not null)
        {
            if (!string.IsNullOrWhiteSpace(metadata.Title))
            {
                book.Title = metadata.Title;
            }

            if (!string.IsNullOrWhiteSpace(metadata.Author))
            {
                book.Author = metadata.Author;
            }

            book.Isbn = metadata.Isbn ?? book.Isbn;
        }

        await TryMatchWithCatalogAsync(dbContext, book, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Best-effort мост между мирами (ТЗ §4.3): сначала ISBN, затем title+author.
    // Матчим по каталожным записям в своей БД; не нашли — книга живёт отдельной
    // записью только с FileId (обязательный fallback из ТЗ).
    private async Task TryMatchWithCatalogAsync(
        CatalogDbContext dbContext,
        Book uploaded,
        CancellationToken cancellationToken)
    {
        Book? candidate = null;

        if (uploaded.Isbn is not null)
        {
            candidate = await dbContext.Books.SingleOrDefaultAsync(
                b => b.Id != uploaded.Id && b.ExternalId != null && b.FileId == null && b.Isbn == uploaded.Isbn,
                cancellationToken);
        }

        if (candidate is null && uploaded.Title.Length > 0 && uploaded.Author.Length > 0)
        {
            var title = uploaded.Title.ToLower();
            var author = uploaded.Author.ToLower();
            candidate = await dbContext.Books.FirstOrDefaultAsync(
                b => b.Id != uploaded.Id
                    && b.ExternalId != null
                    && b.FileId == null
                    && b.Title.ToLower() == title
                    && b.Author.ToLower() == author,
                cancellationToken);
        }

        if (candidate is null)
        {
            return;
        }

        // «Золотой случай» (§4.1): каталожная запись получает файл, дубль исчезает.
        candidate.FileId = uploaded.FileId;
        candidate.Isbn ??= uploaded.Isbn;

        var uploadedShelves = await dbContext.UserBooks
            .Where(ub => ub.BookId == uploaded.Id)
            .ToListAsync(cancellationToken);
        var candidateUserIds = await dbContext.UserBooks
            .Where(ub => ub.BookId == candidate.Id)
            .Select(ub => ub.UserId)
            .ToListAsync(cancellationToken);

        foreach (var shelf in uploadedShelves)
        {
            if (candidateUserIds.Contains(shelf.UserId))
            {
                // У пользователя каталожная книга уже на полке — wishlist «самоисполнился» (§4.4).
                dbContext.UserBooks.Remove(shelf);
            }
            else
            {
                dbContext.UserBooks.Remove(shelf);
                dbContext.UserBooks.Add(new UserBook
                {
                    UserId = shelf.UserId,
                    BookId = candidate.Id,
                    Status = shelf.Status,
                    IsFavorite = shelf.IsFavorite,
                    CfiPosition = shelf.CfiPosition,
                    Progress = shelf.Progress,
                    AddedAtUtc = shelf.AddedAtUtc,
                    LastReadAtUtc = shelf.LastReadAtUtc,
                });
            }
        }

        dbContext.Books.Remove(uploaded);
        logger.LogInformation(
            "Uploaded book {UploadedId} matched catalog record {CandidateId} ({Title})",
            uploaded.Id,
            candidate.Id,
            candidate.Title);
    }
}
