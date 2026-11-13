using YarPhoenix.Catalog.Contracts;

namespace YarPhoenix.Catalog.Application;

// Абстракция над внешним источником метаданных (ТЗ §5.1): позволяет добавить
// провайдера, не трогая домен. Сейчас единственная реализация — Open Library.
public interface IBookMetadataProvider
{
    Task<BookListResponse> SearchAsync(BookSearchQuery query, CancellationToken cancellationToken);

    Task<BookDetailsResponse?> GetDetailsAsync(string externalId, CancellationToken cancellationToken);

    // Родство провайдеров (ТЗ §4.1): по OL-записи узнаём, есть ли открытый скан
    // на Internet Archive и его ocaid-идентификаторы. null → не PD-читаемая работа.
    Task<PublicDomainCandidate?> FindPublicDomainCandidateAsync(
        string externalId,
        CancellationToken cancellationToken);
}
