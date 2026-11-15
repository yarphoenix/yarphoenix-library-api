namespace YarPhoenix.Catalog.Contracts;

public sealed record BookSummaryResponse(
    string Provider,
    string ExternalId,
    string Title,
    string Author,
    int? CoverId,
    int? FirstPublishYear,
    // Книга входит в читаемую коллекцию: у неё есть текст, «Читать» сработает.
    bool IsReadable);

public sealed record BookListResponse(
    IReadOnlyList<BookSummaryResponse> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);

public sealed record BookDetailsResponse(
    string Provider,
    string ExternalId,
    string Title,
    string Author,
    int? CoverId,
    string? Description,
    int? FirstPublishYear,
    IReadOnlyList<string> Subjects,
    bool HasReadableFile,
    // Книга читаемой коллекции: файла может ещё не быть, но «Читать» подтянет его из
    // Standard Ebooks или Project Gutenberg.
    bool IsPublicDomain);
