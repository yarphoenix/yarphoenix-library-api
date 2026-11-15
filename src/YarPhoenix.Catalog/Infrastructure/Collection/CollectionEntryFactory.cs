using System.Globalization;
using System.Text.RegularExpressions;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Domain;
using YarPhoenix.Catalog.Infrastructure.OpenLibrary;

namespace YarPhoenix.Catalog.Infrastructure.Collection;

internal sealed record CollectionEntry(
    string ExternalId,
    string Title,
    string Author,
    int CoverId,
    int? FirstPublishYear,
    string[] Subjects,
    string[] Genres,
    int Popularity,
    ReadableSource Source,
    string SourceRef,
    int? FallbackGutenbergId);

// Решает, попадает ли работа Open Library в читаемую коллекцию, и из какого источника брать текст.
internal static partial class CollectionEntryFactory
{
    private const int MaxTextLength = 512;

    public static CollectionEntry? Create(ReadableWork work, GutenbergCatalog catalog, int currentYear)
    {
        var authors = work.Authors.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();

        // Фильтр качества: без названия, автора или обложки карточка на витрине пустая.
        if (string.IsNullOrWhiteSpace(work.Title) || authors.Count == 0 || work.CoverId is not { } coverId)
        {
            return null;
        }

        var author = OpenLibraryProvider.JoinAuthors(authors);

        var edition = GutenbergEditionSelector.Pick(work.Title, work.GutenbergIds, catalog);
        var gutenbergId = edition is not null && IsPublicDomain(edition, author, currentYear)
            ? edition.Id
            : (int?)null;

        var standardEbooksId = work.StandardEbooksIds.FirstOrDefault(id => SlugRegex().IsMatch(id));
        var useStandardEbooks = standardEbooksId is not null
            && IsPublicDomain(standardEbooksId, authors, author, catalog, currentYear);

        // Standard Ebooks — вычитанные издания с современной вёрсткой, поэтому они в приоритете;
        // Gutenberg остаётся запасным вариантом на случай, если файл SE не скачается.
        var (source, sourceRef, fallback) = (useStandardEbooks, gutenbergId) switch
        {
            (true, _) => (ReadableSource.StandardEbooks, standardEbooksId!, gutenbergId),
            (false, { } id) => (ReadableSource.Gutenberg, id.ToString(CultureInfo.InvariantCulture), (int?)null),
            _ => (ReadableSource.None, string.Empty, (int?)null),
        };

        if (source == ReadableSource.None)
        {
            return null;
        }

        return new CollectionEntry(
            work.ExternalId,
            Truncate(work.Title.Trim()),
            Truncate(author),
            coverId,
            work.FirstPublishYear,
            OpenLibrarySubjects.Clean(work.Subjects).ToArray(),
            OpenLibrarySubjects.ToGenres(work.Subjects),
            work.EditionCount,
            source,
            sourceRef,
            fallback);
    }

    private static bool IsPublicDomain(GutenbergEdition edition, string author, int currentYear) =>
        PublicDomainRule.IsPublicDomain(
            edition.People.Select(person => person.DeathYear).ToList(),
            author,
            currentYear);

    // У издания Standard Ebooks годы жизни берутся по именам: авторы — из Open Library, переводчики
    // и иллюстраторы — из slug ("leo-tolstoy/war-and-peace/louise-maude_aylmer-maude").
    private static bool IsPublicDomain(
        string standardEbooksId,
        IReadOnlyList<string> authors,
        string author,
        GutenbergCatalog catalog,
        int currentYear)
    {
        var contributors = standardEbooksId.Split('/')
            .Skip(2)
            .SelectMany(segment => segment.Split('_', StringSplitOptions.RemoveEmptyEntries))
            .Select(slug => slug.Replace('-', ' '));

        var deathYears = authors
            .Concat(contributors)
            .Select(name => catalog.TryGetDeathYear(name, out var year) ? year : null)
            .ToList();

        return PublicDomainRule.IsPublicDomain(deathYears, author, currentYear);
    }

    private static string Truncate(string value) =>
        value.Length <= MaxTextLength ? value : value[..MaxTextLength];

    // Slug попадает в адрес файла — пропускаем только то, что похоже на путь издания SE.
    [GeneratedRegex(@"^[a-z0-9-]+(/[a-z0-9_-]+){1,3}$")]
    private static partial Regex SlugRegex();
}
