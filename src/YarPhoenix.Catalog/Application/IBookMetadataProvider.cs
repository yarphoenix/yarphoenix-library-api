using YarPhoenix.Catalog.Contracts;

namespace YarPhoenix.Catalog.Application;

// Абстракция над внешним источником метаданных (ТЗ §5.1): позволяет добавить
// провайдера, не трогая домен. Сейчас единственная реализация — Open Library.
public interface IBookMetadataProvider
{
    Task<BookListResponse> SearchAsync(BookSearchQuery query, CancellationToken cancellationToken);

    Task<BookDetailsResponse?> GetDetailsAsync(string externalId, CancellationToken cancellationToken);

    // Родство провайдеров (ТЗ §4.1): работы, у которых есть издание в Standard Ebooks или
    // Project Gutenberg, — сырьё для «читаемой коллекции». Самые издаваемые идут первыми.
    Task<ReadableWorksPage> ListReadableWorksAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
