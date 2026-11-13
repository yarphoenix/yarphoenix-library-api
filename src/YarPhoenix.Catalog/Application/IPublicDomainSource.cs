namespace YarPhoenix.Catalog.Application;

// Слот 2 (ТЗ §5.1.2): читаемые PD-файлы из Internet Archive. Отделён от слота
// метаданных (IBookMetadataProvider) — это разные роли одного «родственного» источника.
public interface IPublicDomainSource
{
    // Проверяет доступность элемента и возвращает открытый EPUB. Здесь же живёт
    // фильтр lending (ТЗ §5.1.2): закрытые/DRM-элементы отсекаются → null.
    Task<PublicDomainFile?> ResolveFileAsync(string identifier, CancellationToken cancellationToken);

    // Скачивает файл в память (EPUB — единицы МБ); вызывающий кладёт поток в MinIO.
    Task<Stream> DownloadAsync(string identifier, string fileName, CancellationToken cancellationToken);
}
