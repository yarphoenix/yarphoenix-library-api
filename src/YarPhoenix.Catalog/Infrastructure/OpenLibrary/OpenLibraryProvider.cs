using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Contracts;

namespace YarPhoenix.Catalog.Infrastructure.OpenLibrary;

public sealed partial class OpenLibraryProvider(HttpClient httpClient) : IBookMetadataProvider
{
    public const string ProviderName = "openlibrary";

    private const string SearchFields = "key,title,author_name,cover_i,first_publish_year";

    public async Task<BookListResponse> SearchAsync(
        BookSearchQuery query,
        CancellationToken cancellationToken)
    {
        var q = Uri.EscapeDataString(OpenLibraryQuery.Build(query));
        var url = $"/search.json?q={q}&page={query.Page}&limit={query.PageSize}&fields={SearchFields}";

        var response = await httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(url, cancellationToken)
            ?? new OpenLibrarySearchResponse();

        var items = response.Docs
            .Select(doc => new BookSummaryResponse(
                ProviderName,
                ExtractId(doc.Key),
                doc.Title,
                doc.AuthorNames is { Count: > 0 } ? string.Join(", ", doc.AuthorNames.Take(2)) : string.Empty,
                doc.CoverId,
                doc.FirstPublishYear))
            .ToList();

        var totalPages = (int)Math.Ceiling(response.NumFound / (double)query.PageSize);
        return new BookListResponse(items, query.Page, query.PageSize, response.NumFound, totalPages);
    }

    public async Task<BookDetailsResponse?> GetDetailsAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        using var workResponse = await httpClient.GetAsync($"/works/{externalId}.json", cancellationToken);
        if (workResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        workResponse.EnsureSuccessStatusCode();
        var work = await workResponse.Content.ReadFromJsonAsync<OpenLibraryWork>(cancellationToken);
        if (work is null)
        {
            return null;
        }

        var author = await GetFirstAuthorNameAsync(work, cancellationToken);

        return new BookDetailsResponse(
            ProviderName,
            externalId,
            work.Title,
            author,
            work.Covers is { Count: > 0 } ? work.Covers[0] : null,
            ExtractDescription(work.Description),
            ExtractYear(work.FirstPublishDate),
            work.Subjects?.Take(6).ToList() ?? [],
            HasReadableFile: false,
            // Политику PD (вайтлист) применяет сервисный слой — провайдер лишь фактограф.
            IsPublicDomain: false);
    }

    public async Task<PublicDomainCandidate?> FindPublicDomainCandidateAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        // Поиск по ключу работы: одним запросом узнаём ocaid-идентификаторы и флаг
        // открытого скана (в Works API их нет — они на уровне поискового индекса).
        var q = Uri.EscapeDataString($"key:/works/{externalId}");
        var url = $"/search.json?q={q}&fields=ia,public_scan_b,ebook_access,author_name&limit=1";

        var response = await httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(url, cancellationToken);
        var doc = response?.Docs.FirstOrDefault();

        if (doc is null
            || !doc.PublicScan
            || !string.Equals(doc.EbookAccess, "public", StringComparison.Ordinal)
            || doc.Ia is not { Count: > 0 })
        {
            return null;
        }

        var author = doc.AuthorNames is { Count: > 0 } ? doc.AuthorNames[0] : string.Empty;
        return new PublicDomainCandidate(doc.Ia, author);
    }

    private async Task<string> GetFirstAuthorNameAsync(OpenLibraryWork work, CancellationToken cancellationToken)
    {
        var authorKey = work.Authors?.FirstOrDefault()?.Author?.Key;
        if (string.IsNullOrEmpty(authorKey))
        {
            return string.Empty;
        }

        using var response = await httpClient.GetAsync($"{authorKey}.json", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return string.Empty;
        }

        var author = await response.Content.ReadFromJsonAsync<OpenLibraryAuthor>(cancellationToken);
        return author?.Name ?? string.Empty;
    }

    internal static string ExtractId(string workKey) =>
        workKey.StartsWith("/works/", StringComparison.Ordinal) ? workKey["/works/".Length..] : workKey;

    // description бывает и строкой, и объектом {"value": "..."} — нормализуем.
    internal static string? ExtractDescription(JsonElement? description) =>
        description switch
        {
            { ValueKind: JsonValueKind.String } element => element.GetString(),
            { ValueKind: JsonValueKind.Object } element
                when element.TryGetProperty("value", out var value) => value.GetString(),
            _ => null,
        };

    // first_publish_date — свободный текст ("1869", "Jan 1, 1869"); берём год.
    internal static int? ExtractYear(string? firstPublishDate)
    {
        if (string.IsNullOrWhiteSpace(firstPublishDate))
        {
            return null;
        }

        var match = YearRegex().Match(firstPublishDate);
        return match.Success ? int.Parse(match.Value) : null;
    }

    [GeneratedRegex(@"\d{4}")]
    private static partial Regex YearRegex();
}
