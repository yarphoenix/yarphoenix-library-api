namespace YarPhoenix.Catalog.Application;

public sealed record BookSearchQuery(
    int Page,
    int PageSize,
    string? Text,
    string? Genre,
    string? Language);
