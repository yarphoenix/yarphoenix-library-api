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
            try
            {
                var content = await TryDownloadAsync(url, cancellationToken);
                if (content is not null)
                {
                    return content;
                }
            }
            catch (Exception exception) when (ProviderFailure.Is(exception, cancellationToken))
            {
                // Один адрес не ответил — следующий (зеркало) всё равно пробуем.
                logger.LogWarning(exception, "Ebook source failed for {Url}", url);
            }
        }

        return null;
    }

    public async Task<Stream> DownloadCatalogFeedAsync(CancellationToken cancellationToken)
    {
        HttpRequestException? lastFailure = null;

        foreach (var baseUrl in GutenbergBaseUrls(_options))
        {
            var url = new Uri(baseUrl + CatalogFeedPath);
            try
            {
                using var response = await httpClient.GetAsync(
                    url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Около 20 МБ: буферизуем, чтобы разбор CSV не держал сетевое соединение.
                var buffer = new MemoryStream();
                await response.Content.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;
                return buffer;
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(
                    "Gutenberg catalog feed is unavailable at {Url}: {Reason}", url, exception.Message);
                lastFailure = exception;
            }
        }

        throw lastFailure ?? new HttpRequestException("No Project Gutenberg address is configured.");
    }

    // Адреса в порядке предпочтения; следующий пробуется, если предыдущий не дал EPUB.
    internal static IReadOnlyList<Uri> BuildUrls(ReadableSource source, string sourceRef, CollectionOptions options)
    {
        var standardEbooks = options.StandardEbooksBaseUrl.TrimEnd('/');

        return source switch
        {
            // Без ?source=download Standard Ebooks отдаёт HTML-страницу «загрузка началась», а не файл.
            ReadableSource.StandardEbooks =>
            [
                new Uri($"{standardEbooks}/ebooks/{sourceRef}/downloads/{sourceRef.Replace('/', '_')}.epub?source=download"),
            ],
            ReadableSource.Gutenberg => BuildGutenbergUrls(sourceRef, options),
            _ => [],
        };
    }

    // Основной сайт Project Gutenberg, затем зеркало, если оно задано.
    internal static IReadOnlyList<string> GutenbergBaseUrls(CollectionOptions options)
    {
        string[] candidates = [options.GutenbergBaseUrl, options.GutenbergMirrorBaseUrl];
        return [.. candidates.Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => url.TrimEnd('/'))];
    }

    // Сначала EPUB3 с оглавлением и картинками — с основного сайта, затем с зеркала. Иллюстрированные
    // издания бывают огромными, тогда оба пропускаются по размеру и берётся вариант без картинок.
    // У основного сайта и зеркала разная раскладка адресов одного и того же файла.
    private static Uri[] BuildGutenbergUrls(string id, CollectionOptions options)
    {
        var main = options.GutenbergBaseUrl.TrimEnd('/');
        var mirror = options.GutenbergMirrorBaseUrl.TrimEnd('/');
        var hasMirror = !string.IsNullOrWhiteSpace(mirror);

        List<Uri> urls = [new Uri($"{main}/ebooks/{id}.epub3.images")];
        if (hasMirror)
        {
            urls.Add(new Uri($"{mirror}/cache/epub/{id}/pg{id}-images-3.epub"));
        }

        urls.Add(new Uri($"{main}/ebooks/{id}.epub.noimages"));
        if (hasMirror)
        {
            urls.Add(new Uri($"{mirror}/cache/epub/{id}/pg{id}.epub"));
        }

        return [.. urls];
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
