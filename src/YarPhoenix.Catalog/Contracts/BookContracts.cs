namespace YarPhoenix.Catalog.Contracts;

public sealed record BookSummaryResponse(
    string Provider,
    string ExternalId,
    string Title,
    string Author,
    int? CoverId,
    int? FirstPublishYear);

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
    // PD-книга: файла ещё нет, но его можно подтянуть с Internet Archive по «Читать» (ТЗ §5.1.2).
    bool IsPublicDomain);
