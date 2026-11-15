using YarPhoenix.Catalog.Infrastructure.Collection;

namespace YarPhoenix.Catalog.Tests;

public sealed class GutenbergEditionSelectorTests
{
    private const string Feed = """
        Text#,Type,Issued,Title,Language,Authors,Subjects,LoCC,Bookshelves
        84,Text,,"Frankenstein; Or, The Modern Prometheus",en,"Shelley, Mary Wollstonecraft, 1797-1851",,,
        41445,Text,,"Frankenstein; Or, The Modern Prometheus",en,"Shelley, Mary Wollstonecraft, 1797-1851",,,
        2600,Text,,War and Peace,en,"Tolstoy, Leo, graf, 1828-1910",,,
        2610,Text,,Война и мир. Том 1,ru,"Tolstoy, Leo, graf, 1828-1910",,,
        9000,Sound,,War and Peace,en,"Tolstoy, Leo, graf, 1828-1910",,,
        731,Text,,History of the Decline and Fall of the Roman Empire — Volume 1,en,"Gibbon, Edward, 1737-1794",,,
        25717,Text,,The History of the Decline and Fall of the Roman Empire,en,"Gibbon, Edward, 1737-1794",,,
        135,Text,,Les Misérables,en; fr,"Hugo, Victor, 1802-1885",,,
        """;

    private static readonly GutenbergCatalog Catalog = GutenbergCatalog.Parse(new StringReader(Feed));

    [Fact]
    public void Pick_TakesEnglishText_NotTranslationOrAudio()
    {
        var edition = GutenbergEditionSelector.Pick("War and Peace", [2610, 9000, 2600], Catalog);

        Assert.Equal(2600, edition?.Id);
    }

    [Fact]
    public void Pick_PrefersWholeBookOverSingleVolume()
    {
        var edition = GutenbergEditionSelector.Pick(
            "The History of the Decline and Fall of the Roman Empire", [731, 25717], Catalog);

        Assert.Equal(25717, edition?.Id);
    }

    [Fact]
    public void Pick_MatchesTitleBeforeSubtitle_AndTakesLowestNumberAmongEquals()
    {
        var edition = GutenbergEditionSelector.Pick("Frankenstein", [41445, 84], Catalog);

        Assert.Equal(84, edition?.Id);
    }

    [Fact]
    public void Pick_FallsBackToVolume_WhenThereIsNothingElse()
    {
        var edition = GutenbergEditionSelector.Pick("Decline and Fall of the Roman Empire", [731], Catalog);

        Assert.Equal(731, edition?.Id);
    }

    [Fact]
    public void Pick_ReturnsNull_WhenNoEnglishTextEditionExists()
    {
        // Двуязычное издание, русский перевод, аудио и номер, которого нет в каталоге.
        var edition = GutenbergEditionSelector.Pick("Les Misérables", [135, 2610, 9000, 999999], Catalog);

        Assert.Null(edition);
    }

    [Theory]
    [InlineData("History of Rome — Volume 1", true)]
    [InlineData("Don Quixote, Vol. II", true)]
    [InlineData("The Memoirs, Part 3", true)]
    [InlineData("The Jungle Book", false)]
    [InlineData("War and Peace", false)]
    public void IsPartial_RecognizesVolumesAndParts(string title, bool expected)
    {
        Assert.Equal(expected, GutenbergEditionSelector.IsPartial(title));
    }
}
