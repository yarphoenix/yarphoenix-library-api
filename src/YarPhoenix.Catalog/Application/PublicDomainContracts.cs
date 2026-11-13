namespace YarPhoenix.Catalog.Application;

// Кандидат на «золотой случай» (ТЗ §4.1, provider kinship): Open Library-запись
// PD-книги несёт идентификаторы Internet Archive (ocaid) — родной мост каталог → файл
// без сопоставления по названию. Список, а не один id: у одной работы бывает
// несколько сканов (часть — lending или мусорные google-скан-EPUB), перебираем по порядку.
public sealed record PublicDomainCandidate(IReadOnlyList<string> Identifiers, string Author);

// Открытый (не lending) файл на Internet Archive, готовый к скачиванию в MinIO.
public sealed record PublicDomainFile(string Identifier, string FileName, long Size);
