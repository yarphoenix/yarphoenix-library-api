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

        await EnrichFromCatalogAsync(dbContext, book, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Best-effort мост между мирами (ТЗ §4.3): сначала ISBN, затем title+author — ищем каталожную
    // запись той же книги и берём у неё то, чего нет в самом файле: обложку, описание, год.
    // Не нашли — книга живёт с метаданными из OPF.
    private async Task EnrichFromCatalogAsync(
        CatalogDbContext dbContext,
        Book uploaded,
        CancellationToken cancellationToken)
    {
        Book? catalogRecord = null;

        if (uploaded.Isbn is not null)
        {
            catalogRecord = await dbContext.Books.AsNoTracking().FirstOrDefaultAsync(
                b => b.Id != uploaded.Id && b.ExternalId != null && b.Isbn == uploaded.Isbn,
                cancellationToken);
        }

        if (catalogRecord is null && uploaded.Title.Length > 0 && uploaded.Author.Length > 0)
        {
            var title = uploaded.Title.ToLower();
            var author = uploaded.Author.ToLower();
            catalogRecord = await dbContext.Books.AsNoTracking().FirstOrDefaultAsync(
                b => b.Id != uploaded.Id
                    && b.ExternalId != null
                    && b.Title.ToLower() == title
                    && b.Author.ToLower() == author,
                cancellationToken);
        }

        if (catalogRecord is null)
        {
            return;
        }

        CopyCatalogMetadata(uploaded, catalogRecord);
        logger.LogInformation(
            "Uploaded book {UploadedId} took metadata from catalog record {CatalogId} ({Title})",
            uploaded.Id,
            catalogRecord.Id,
            catalogRecord.Title);
    }

    // Из совпадения копируются только метаданные. Файл остаётся у личной записи загрузившего:
    // каталожная запись общая для всех, и чужой EPUB (другое издание, перевод, чьи-то пометки)
    // в неё попадать не должен — иначе его начинают читать все.
    internal static void CopyCatalogMetadata(Book uploaded, Book catalogRecord)
    {
        uploaded.CoverId ??= catalogRecord.CoverId;
        uploaded.FirstPublishYear ??= catalogRecord.FirstPublishYear;

        if (string.IsNullOrEmpty(uploaded.Description) && !string.IsNullOrEmpty(catalogRecord.Description))
        {
            uploaded.Description = catalogRecord.Description;
        }

        if (uploaded.Subjects.Length == 0)
        {
            uploaded.Subjects = catalogRecord.Subjects;
        }
    }
}
