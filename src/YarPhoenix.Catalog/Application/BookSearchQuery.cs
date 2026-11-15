namespace YarPhoenix.Catalog.Application;

public sealed record BookSearchQuery(
    int Page,
    int PageSize,
    string? Text,
    string? Genre,
    string? Language,
    CatalogScope Scope = CatalogScope.Collection);

// Что показывает каталог: читаемую коллекцию из своей БД (по умолчанию) или весь Open Library.
public enum CatalogScope
{
    Collection,
    All,
}
