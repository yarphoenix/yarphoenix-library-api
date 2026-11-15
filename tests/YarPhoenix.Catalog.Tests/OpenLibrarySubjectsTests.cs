using YarPhoenix.Catalog.Infrastructure.OpenLibrary;

namespace YarPhoenix.Catalog.Tests;

public sealed class OpenLibrarySubjectsTests
{
    [Fact]
    public void Clean_DropsMachineTagsServiceLabelsAndDuplicates()
    {
        string[] subjects =
        [
            "Fiction",
            "series:Harry_Potter",
            "nyt:hardcover-fiction=2011-01-01",
            "Accessible book",
            "fiction",
            "  Wizards  ",
            "",
        ];

        Assert.Equal(["Fiction", "Wizards"], OpenLibrarySubjects.Clean(subjects));
    }

    [Fact]
    public void Clean_KeepsHumanSubjectsWithColon()
    {
        string[] subjects = ["Fiction: general", "Alice (fictitious character : carroll), fiction"];

        Assert.Equal(subjects, OpenLibrarySubjects.Clean(subjects));
    }

    [Fact]
    public void Clean_RespectsLimit()
    {
        var subjects = Enumerable.Range(1, 10).Select(number => $"Subject {number}");

        Assert.Equal(["Subject 1", "Subject 2", "Subject 3"], OpenLibrarySubjects.Clean(subjects, limit: 3));
    }

    [Fact]
    public void Clean_ReturnsEmpty_WhenSubjectsAreMissing()
    {
        Assert.Empty(OpenLibrarySubjects.Clean(null));
    }

    [Fact]
    public void ToGenres_MapsSubjectsToCatalogGenreKeys()
    {
        string[] subjects = ["Science fiction", "Detective and mystery stories", "Love stories", "Nonfiction"];

        var genres = OpenLibrarySubjects.ToGenres(subjects);

        Assert.Equal(["fiction", "mystery", "romance", "scifi"], genres.Order());
    }

    [Fact]
    public void ToGenres_DoesNotTreatLiteraryCriticismAsHistory()
    {
        string[] subjects = ["History and criticism", "English literature"];

        Assert.Empty(OpenLibrarySubjects.ToGenres(subjects));
    }

    [Fact]
    public void ToGenres_IgnoresMachineTags()
    {
        string[] subjects = ["series:Fantasy_Kingdom", "collectionID:romance"];

        Assert.Empty(OpenLibrarySubjects.ToGenres(subjects));
    }

    [Fact]
    public void ToGenres_OnlyProducesKeysTheCatalogFilterAccepts()
    {
        string[] subjects =
        [
            "Science fiction", "Fantasy", "Classics", "Suspense", "Detective", "Mythology", "History",
            "Autobiography", "Self-help", "Romance",
        ];

        var genres = OpenLibrarySubjects.ToGenres(subjects);

        Assert.Equal(OpenLibraryQuery.GenreSubjects.Keys.Order(), genres.Order());
    }

    [Fact]
    public void ToGenres_NeedsMoreThanOneStrayTag_WhenWorkHasManySubjects()
    {
        // Как у «Алисы в Стране чудес»: полторы сотни тем, среди них одна случайная «Science fiction».
        var subjects = Enumerable.Range(1, 150).Select(number => $"Topic {number}")
            .Concat(["Science fiction", "Classics"])
            .Concat(["Fantasy", "Fantasy fiction", "Fantasy & Magic", "Fiction, fantasy, general"]);

        var genres = OpenLibrarySubjects.ToGenres(subjects);

        Assert.Contains("fantasy", genres);
        Assert.DoesNotContain("scifi", genres);
        // Широкому жанру достаточно одной темы.
        Assert.Contains("classic", genres);
    }
}
