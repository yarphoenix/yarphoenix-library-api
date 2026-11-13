using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Hybrid;
using YarPhoenix.Catalog.Application;

namespace YarPhoenix.Catalog.Infrastructure.InternetArchive;

// Слот 2 (ТЗ §5.1.2): поиск/метаданные/скачивание — всё простой HTTP GET из C#.
// Метаданные кэшируем; сам файл после первой загрузки живёт в MinIO и IA больше
// не дёргается (единственный настоящий дедуп — сохранённый FileId у книги).
public sealed class InternetArchiveSource(HttpClient httpClient, HybridCache cache) : IPublicDomainSource
{
    private static readonly HybridCacheEntryOptions MetadataCacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(6),
        LocalCacheExpiration = TimeSpan.FromMinutes(30),
    };

    public async Task<PublicDomainFile?> ResolveFileAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        var metadata = await cache.GetOrCreateAsync(
            $"ia:meta:{identifier}",
            async token => await FetchMetadataAsync(identifier, token),
            MetadataCacheOptions,
            cancellationToken: cancellationToken);

        if (metadata.Files is null || IaLendingFilter.IsRestricted(metadata))
        {
            return null;
        }

        var epub = IaFileSelector.PickOpenEpub(metadata.Files);
        if (epub is null)
        {
            return null;
        }

        var size = long.TryParse(epub.Size, out var parsed) ? parsed : 0;
        return new PublicDomainFile(identifier, epub.Name, size);
    }

    public async Task<Stream> DownloadAsync(
        string identifier,
        string fileName,
        CancellationToken cancellationToken)
    {
        // archive.org/download/{id}/{file} отвечает 302 на зеркало — HttpClient
        // следует редиректу сам. Буферизуем в память (EPUB — единицы МБ), чтобы
        // отдать поток с известной длиной в MinIO.
        using var response = await httpClient.GetAsync(
            $"/download/{identifier}/{Uri.EscapeDataString(fileName)}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var buffer = new MemoryStream();
        await response.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    private async Task<IaMetadataResponse> FetchMetadataAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"/metadata/{identifier}", cancellationToken);

        // 404/несуществующий элемент — не ошибка: пустой ответ (Files=null → отсеётся).
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new IaMetadataResponse();
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IaMetadataResponse>(cancellationToken)
            ?? new IaMetadataResponse();
    }
}
