using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Domain;
using YarPhoenix.Catalog.Infrastructure.Collection;

namespace YarPhoenix.Catalog.Tests;

public sealed class CollectionEntryFactoryTests
{
    private const int CurrentYear = 2026;

    private const string Feed = """
        Text#,Type,Issued,Title,Language,Authors,Subjects,LoCC,Bookshelves
        1342,Text,,Pride and Prejudice,en,"Austen, Jane, 1775-1817",,,
        2600,Text,,War and Peace,en,"Tolstoy, Leo, graf, 1828-1910; Maude, Louise, 1855-1939 [Translator]; Maude, Aylmer, 1858-1938 [Translator]",,,
        5000,Text,,A Modern Novel,en,"Writer, Recent, 1920-1990",,,
        6000,Text,,Old Tales,en,Anonymous,,,
        """;

    private static readonly GutenbergCatalog Catalog = GutenbergCatalog.Parse(new StringReader(Feed));

    private static ReadableWork Work(
        string title = "Pride and Prejudice",
        string[]? authors = null,
        int? coverId = 14348537,
        string[]? standardEbooks = null,
        int[]? gutenberg = null) =>
        new(
            "OL66554W",
            title,
            authors ?? ["Jane Austen"],
            coverId,
            1813,
            ["Fiction", "series:Bennet_Family", "Love stories"],
            4042,
            standardEbooks ?? [],
            gutenberg ?? []);

    private static CollectionEntry? Create(ReadableWork work) =>
        CollectionEntryFactory.Create(work, Catalog, CurrentYear);

    [Fact]
    public void Create_BuildsEntryFromGutenbergEdition()
    {
        var entry = Create(Work(gutenberg: [1342]));

        Assert.NotNull(entry);
        Assert.Equal("OL66554W", entry.ExternalId);
        Assert.Equal("Pride and Prejudice", entry.Title);
        Assert.Equal("Jane Austen", entry.Author);
        Assert.Equal(14348537, entry.CoverId);
        Assert.Equal(4042, entry.Popularity);
        Assert.Equal(ReadableSource.Gutenberg, entry.Source);
        Assert.Equal("1342", entry.SourceRef);
        Assert.Null(entry.FallbackGutenbergId);
        Assert.Equal(["Fiction", "Love stories"], entry.Subjects);
        Assert.Equal(["fiction", "romance"], entry.Genres.Order());
    }

    [Fact]
    public void Create_PrefersStandardEbooks_AndKeepsGutenbergAsFallback()
    {
        var entry = Create(Work(standardEbooks: ["jane-austen/pride-and-prejudice"], gutenberg: [1342]));

        Assert.NotNull(entry);
        Assert.Equal(ReadableSource.StandardEbooks, entry.Source);
        Assert.Equal("jane-austen/pride-and-prejudice", entry.SourceRef);
        Assert.Equal(1342, entry.FallbackGutenbergId);
    }

    [Fact]
    public void Create_AcceptsStandardEbooksEdition_WhenItsTranslatorsDiedLongAgo()
    {
        var entry = Create(Work(
            title: "War and Peace",
            authors: ["Leo Tolstoy"],
            standardEbooks: ["leo-tolstoy/war-and-peace/louise-maude_aylmer-maude"]));

        Assert.NotNull(entry);
        Assert.Equal(ReadableSource.StandardEbooks, entry.Source);
        Assert.Null(entry.FallbackGutenbergId);
    }

    [Fact]
    public void Create_FallsBackToGutenberg_WhenStandardEbooksTranslatorDiedRecently()
    {
        var entry = Create(Work(
            title: "War and Peace",
            authors: ["Leo Tolstoy"],
            standardEbooks: ["leo-tolstoy/war-and-peace/recent-writer"],
            gutenberg: [2600]));

        Assert.NotNull(entry);
        Assert.Equal(ReadableSource.Gutenberg, entry.Source);
        Assert.Equal("2600", entry.SourceRef);
    }

    [Fact]
    public void Create_IgnoresMalformedStandardEbooksSlug()
    {
        var entry = Create(Work(standardEbooks: ["../../etc/passwd"], gutenberg: [1342]));

        Assert.NotNull(entry);
        Assert.Equal(ReadableSource.Gutenberg, entry.Source);
    }

    [Fact]
    public void Create_SkipsWorkOfRecentlyDeceasedAuthor()
    {
        Assert.Null(Create(Work(title: "A Modern Novel", authors: ["Recent Writer"], gutenberg: [5000])));
    }

    [Fact]
    public void Create_SkipsWork_WhenDeathYearIsUnknownAndAuthorIsNotWhitelisted()
    {
        Assert.Null(Create(Work(title: "Old Tales", authors: ["Anonymous"], gutenberg: [6000])));
    }

    [Fact]
    public void Create_SkipsWorkWithoutReadableEdition()
    {
        Assert.Null(Create(Work()));
    }

    [Fact]
    public void Create_SkipsWorkWithoutCover()
    {
        Assert.Null(Create(Work(coverId: null, gutenberg: [1342])));
    }

    [Fact]
    public void Create_SkipsWorkWithoutAuthor()
    {
        Assert.Null(Create(Work(authors: [" "], gutenberg: [1342])));
    }

    [Fact]
    public void Create_SkipsWorkWithoutTitle()
    {
        Assert.Null(Create(Work(title: " ", gutenberg: [1342])));
    }
}
