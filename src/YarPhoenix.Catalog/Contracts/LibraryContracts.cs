namespace YarPhoenix.Catalog.Contracts;

public sealed record LibraryBookResponse(
    Guid BookId,
    string Title,
    string Author,
    int? CoverId,
    string? Provider,
    string? ExternalId,
    bool HasReadableFile,
    string Status,
    bool IsFavorite,
    double Progress,
    string? CfiPosition,
    DateTime AddedAtUtc,
    DateTime? LastReadAtUtc);

public sealed record AddToLibraryRequest(string Provider, string ExternalId);

public sealed record StartReadingRequest(string Provider, string ExternalId);

public sealed record UpdateShelfRequest(string? Status, bool? IsFavorite);

public sealed record ProgressRequest(string CfiPosition, double Progress, string TimeZoneId);

public sealed record LibraryStatsResponse(
    int Total,
    int Reading,
    int WantToRead,
    int Read,
    int Favorites,
    int CurrentStreak,
    int LongestStreak,
    IReadOnlyList<int> Achievements);
