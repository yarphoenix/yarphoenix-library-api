using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Domain;

namespace YarPhoenix.Catalog.Infrastructure.Collection;

// Источник файлов читаемой коллекции: вычитанные EPUB Standard Ebooks и Project Gutenberg вместо
// авто-EPUB из OCR-сканов Internet Archive (осознанное отступление от ТЗ §5.1.2). Файл качается
// один раз по «Читать» и дальше живёт в MinIO.
public sealed class CuratedEbookSource(
    HttpClient httpClient,
    IOptions<CollectionOptions> options,
    ILogger<CuratedEbookSource> logger) : IPublicDomainSource
{
    private const string CatalogFeedPath = "/cache/epub/feeds/pg_catalog.csv";

    private readonly CollectionOptions _options = options.Value;

    public async Task<Stream?> DownloadAsync(
        ReadableSource source,
        string sourceRef,
        CancellationToken cancellationToken)
    {
        foreach (var url in BuildUrls(source, sourceRef, _options))
        {
            var content = await TryDownloadAsync(url, cancellationToken);
            if (content is not null)
            {
                return content;
            }
        }

        return null;
    }

    public async Task<Stream> DownloadCatalogFeedAsync(CancellationToken cancellationToken)
    {
        var url = new Uri(_options.GutenbergBaseUrl.TrimEnd('/') + CatalogFeedPath);
        using var response = await httpClient.GetAsync(
            url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Около 20 МБ: буферизуем, чтобы разбор CSV не держал сетевое соединение.
        var buffer = new MemoryStream();
        await response.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    // Адреса в порядке предпочтения; следующий пробуется, если предыдущий не дал EPUB.
    internal static IReadOnlyList<Uri> BuildUrls(ReadableSource source, string sourceRef, CollectionOptions options)
    {
        var gutenberg = options.GutenbergBaseUrl.TrimEnd('/');
        var standardEbooks = options.StandardEbooksBaseUrl.TrimEnd('/');

        return source switch
        {
            // Без ?source=download Standard Ebooks отдаёт HTML-страницу «загрузка началась», а не файл.
            ReadableSource.StandardEbooks =>
            [
                new Uri($"{standardEbooks}/ebooks/{sourceRef}/downloads/{sourceRef.Replace('/', '_')}.epub?source=download"),
            ],
            // EPUB3 с оглавлением; иллюстрированные издания бывают огромными — тогда вариант без картинок.
            ReadableSource.Gutenberg =>
            [
                new Uri($"{gutenberg}/ebooks/{sourceRef}.epub3.images"),
                new Uri($"{gutenberg}/ebooks/{sourceRef}.epub.noimages"),
            ],
            _ => [],
        };
    }

    // EPUB — это ZIP-архив: по сигнатуре отличаем файл от HTML-заглушки с кодом 200.
    internal static bool LooksLikeEpub(ReadOnlySpan<byte> content) =>
        content.Length > 4 && content[0] == 'P' && content[1] == 'K' && content[2] == 3 && content[3] == 4;

    private async Task<MemoryStream?> TryDownloadAsync(Uri url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Ebook source answered {StatusCode} for {Url}", (int)response.StatusCode, url);
            return null;
        }

        if (response.Content.Headers.ContentLength > _options.MaxFileBytes)
        {
            logger.LogInformation(
                "Skipping {Url}: {Bytes} bytes is over the {Limit} byte limit",
                url,
                response.Content.Headers.ContentLength,
                _options.MaxFileBytes);
            return null;
        }

        var buffer = new MemoryStream();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        var chunk = new byte[81920];
        int read;
        while ((read = await body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            // Длина в заголовке могла отсутствовать — лимит проверяется и по факту.
            if (buffer.Length + read > _options.MaxFileBytes)
            {
                logger.LogInformation("Skipping {Url}: the file is over the size limit", url);
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        if (!LooksLikeEpub(buffer.GetBuffer().AsSpan(0, (int)buffer.Length)))
        {
            logger.LogWarning("Ebook source returned something other than an EPUB for {Url}", url);
            return null;
        }

        buffer.Position = 0;
        return buffer;
    }
}
