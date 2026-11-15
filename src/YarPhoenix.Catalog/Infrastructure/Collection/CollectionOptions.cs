namespace YarPhoenix.Catalog.Infrastructure.Collection;

// Настройки «читаемой коллекции»: откуда берутся файлы и как идёт фоновый импорт.
public sealed class CollectionOptions
{
    public const string SectionName = "Collection";

    // Фоновый импорт; выключается для офлайн-разработки.
    public bool ImportEnabled { get; set; } = true;

    public string StandardEbooksBaseUrl { get; set; } = "https://standardebooks.org";

    // В конфиге, чтобы можно было переключиться на зеркало Project Gutenberg.
    public string GutenbergBaseUrl { get; set; } = "https://www.gutenberg.org";

    public int PageSize { get; set; } = 100;

    // Пауза между запросами к Open Library: импорт не должен выглядеть для неё нагрузкой.
    public int RequestDelayMilliseconds { get; set; } = 1000;

    // Как часто коллекция пересобирается после первого полного прохода.
    public int RefreshDays { get; set; } = 7;

    // EPUB крупнее — иллюстрированное издание на десятки мегабайт, которое через прокси качается
    // дольше минуты. Вместо него берётся вариант того же издания без картинок.
    public long MaxFileBytes { get; set; } = 12 * 1024 * 1024;
}
