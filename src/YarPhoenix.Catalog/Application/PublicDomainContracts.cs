namespace YarPhoenix.Catalog.Application;

// Работа Open Library, у которой есть готовое электронное издание (ТЗ §4.1, родство
// провайдеров): идентификаторы Standard Ebooks и Project Gutenberg — мост «каталог → файл»
// без сопоставления по названию.
public sealed record ReadableWork(
    string ExternalId,
    string Title,
    IReadOnlyList<string> Authors,
    int? CoverId,
    int? FirstPublishYear,
    IReadOnlyList<string> Subjects,
    int EditionCount,
    IReadOnlyList<string> StandardEbooksIds,
    IReadOnlyList<int> GutenbergIds);

public sealed record ReadableWorksPage(IReadOnlyList<ReadableWork> Works, int Total);
