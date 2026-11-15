using YarPhoenix.Catalog.Domain;

namespace YarPhoenix.Catalog.Application;

// Слот 2: читаемые файлы книг в общественном достоянии. Отделён от слота метаданных
// (IBookMetadataProvider) — искать книгу и отдавать её текст умеют разные источники.
public interface IPublicDomainSource
{
    // Скачивает EPUB в память (единицы МБ); вызывающий кладёт поток в MinIO. null — источник
    // ответил, но файла нет: адрес не подошёл, файл слишком большой или пришёл не EPUB.
    Task<Stream?> DownloadAsync(ReadableSource source, string sourceRef, CancellationToken cancellationToken);

    // Каталог источника (pg_catalog.csv Project Gutenberg): язык и тип изданий, годы жизни авторов.
    Task<Stream> DownloadCatalogFeedAsync(CancellationToken cancellationToken);
}
