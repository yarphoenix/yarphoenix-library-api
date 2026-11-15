namespace YarPhoenix.Catalog.Infrastructure.Collection;

// Настройки «читаемой коллекции»: откуда берутся файлы и как идёт фоновый импорт.
public sealed class CollectionOptions
{
    public const string SectionName = "Collection";

    // Фоновый импорт; выключается для офлайн-разработки.
    public bool ImportEnabled { get; set; } = true;

    public string StandardEbooksBaseUrl { get; set; } = "https://standardebooks.org";

    // Основной сайт Project Gutenberg: адреса вида /ebooks/{id}.epub3.images.
    public string GutenbergBaseUrl { get; set; } = "https://www.gutenberg.org";

    // Официальное зеркало для автоматических загрузок: к нему обращаемся, когда основной сайт
    // не отдал файл (он бывает недоступен целиком и отвечает 403 на любой запрос). Раскладка
    // другая: /cache/epub/{id}/pg{id}-images-3.epub. По HTTPS зеркало не работает — у него нет
    // сертификата на это имя, сам Gutenberg указывает его адрес с http. Пустая строка
    // отключает зеркало.
    public string GutenbergMirrorBaseUrl { get; set; } = "http://aleph.gutenberg.org";

    public int PageSize { get; set; } = 100;

    // Пауза между запросами к Open Library: импорт не должен выглядеть для неё нагрузкой.
    public int RequestDelayMilliseconds { get; set; } = 1000;

    // Как часто коллекция пересобирается после первого полного прохода.
    public int RefreshDays { get; set; } = 7;

    // EPUB крупнее — иллюстрированное издание на десятки мегабайт, которое через прокси качается
    // дольше минуты. Вместо него берётся вариант того же издания без картинок.
    public long MaxFileBytes { get; set; } = 12 * 1024 * 1024;
}
