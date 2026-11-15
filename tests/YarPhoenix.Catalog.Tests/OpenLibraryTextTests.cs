using YarPhoenix.Catalog.Infrastructure.OpenLibrary;

namespace YarPhoenix.Catalog.Tests;

public sealed class OpenLibraryTextTests
{
    [Fact]
    public void CleanDescription_DropsSourceNotesAndReferenceDefinitions()
    {
        const string raw =
            "A novel of manners. ([source][1])\r\n\r\n[1]: https://en.wikipedia.org/wiki/Pride_and_Prejudice";

        Assert.Equal("A novel of manners.", OpenLibraryText.CleanDescription(raw));
    }

    [Fact]
    public void CleanDescription_CutsEverythingAfterRule()
    {
        const string raw =
            "The story of Elizabeth Bennet.\n\n----------\nAlso contained in:\n\n - [Complete Novels][1]\n\n"
            + "[1]: https://openlibrary.org/works/OL1W";

        Assert.Equal("The story of Elizabeth Bennet.", OpenLibraryText.CleanDescription(raw));
    }

    [Fact]
    public void CleanDescription_UnwrapsLinksAndBold()
    {
        const string raw =
            "**Pride and Prejudice** is a novel by [Jane Austen](https://example.org/austen), see [her works][2].";

        Assert.Equal(
            "Pride and Prejudice is a novel by Jane Austen, see her works.",
            OpenLibraryText.CleanDescription(raw));
    }

    [Fact]
    public void CleanDescription_NormalizesLineBreaks()
    {
        const string raw = "First paragraph.  \r\n\r\n\r\n\r\nSecond paragraph.\r\n";

        Assert.Equal("First paragraph.\n\nSecond paragraph.", OpenLibraryText.CleanDescription(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("----------\nSee also: other editions")]
    public void CleanDescription_ReturnsNull_WhenNothingIsLeft(string? raw)
    {
        Assert.Null(OpenLibraryText.CleanDescription(raw));
    }
}
