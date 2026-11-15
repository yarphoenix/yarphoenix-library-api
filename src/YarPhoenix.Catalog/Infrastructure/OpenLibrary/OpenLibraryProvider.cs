using System.Globalization;
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

    private const string ReadableWorksQuery = "id_standard_ebooks:* OR id_project_gutenberg:*";

    private const string ReadableWorksFields =
        "key,title,author_name,cover_i,first_publish_year,subject,edition_count,"
        + "id_project_gutenberg,id_standard_ebooks";

    // Сколько тем показывается на странице книги.
    private const int DetailsSubjects = 6;

    public async Task<BookListResponse> SearchAsync(
        BookSearchQuery query,
        CancellationToken cancellationToken)
    {
        var q = Uri.EscapeDataString(OpenLibraryQuery.Build(query));
        var url = $"/search.json?q={q}&page={query.Page}&limit={query.PageSize}&fields={SearchFields}";

        var response = await httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(url, cancellationToken)
            ?? new OpenLibrarySearchResponse();

        // Запись без автора и без обложки — заготовка, а не книга: на витрине это пустая карточка.
        // Страница из-за этого может оказаться короче PageSize; Total остаётся числом Open Library.
        var items = response.Docs
            .Where(doc => doc.AuthorNames is { Count: > 0 } || NormalizeCover(doc.CoverId) is not null)
            .Select(doc => new BookSummaryResponse(
                ProviderName,
                ExtractId(doc.Key),
                doc.Title,
                JoinAuthors(doc.AuthorNames),
                NormalizeCover(doc.CoverId),
                doc.FirstPublishYear,
                // Читаемость знает только наша БД — её проставляет сервисный слой.
                IsReadable: false))
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
            // На месте удалённых обложек Open Library оставляет -1 или null.
            work.Covers?.FirstOrDefault(cover => cover > 0),
            OpenLibraryText.CleanDescription(ExtractDescription(work.Description)),
            ExtractYear(work.FirstPublishDate),
            OpenLibrarySubjects.Clean(work.Subjects, DetailsSubjects),
            HasReadableFile: false,
            // Читаемость и статус коллекции определяет сервисный слой — провайдер лишь фактограф.
            IsPublicDomain: false);
    }

    public async Task<ReadableWorksPage> ListReadableWorksAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // sort=editions: порядок стабилен от страницы к странице, а самые издаваемые книги
        // попадают в коллекцию первыми — витрина полезна ещё до конца импорта.
        var q = Uri.EscapeDataString(ReadableWorksQuery);
        var url = $"/search.json?q={q}&page={page}&limit={pageSize}&sort=editions&fields={ReadableWorksFields}";

        var response = await httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(url, cancellationToken)
            ?? new OpenLibrarySearchResponse();

        var works = response.Docs
            .Select(doc => new ReadableWork(
                ExtractId(doc.Key),
                doc.Title,
                doc.AuthorNames ?? [],
                NormalizeCover(doc.CoverId),
                doc.FirstPublishYear,
                doc.Subjects ?? [],
                doc.EditionCount,
                doc.StandardEbooksIds ?? [],
                ParseGutenbergIds(doc.GutenbergIds)))
            .ToList();

        return new ReadableWorksPage(works, response.NumFound);
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

    internal static string JoinAuthors(IReadOnlyList<string>? authors) =>
        authors is { Count: > 0 } ? string.Join(", ", authors.Take(2)) : string.Empty;

    // cover_i / covers[] у записей без обложки бывают -1 или 0: такой id даёт битую картинку.
    internal static int? NormalizeCover(int? coverId) => coverId > 0 ? coverId : null;

    internal static List<int> ParseGutenbergIds(IEnumerable<string>? ids)
    {
        var result = new List<int>();
        foreach (var raw in ids ?? [])
        {
            if (int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
                && id > 0
                && !result.Contains(id))
            {
                result.Add(id);
            }
        }

        return result;
    }

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
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : null;
    }

    [GeneratedRegex(@"\d{4}")]
    private static partial Regex YearRegex();
}
