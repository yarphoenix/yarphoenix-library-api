using YarPhoenix.Catalog.Application;

namespace YarPhoenix.Catalog.Tests;

public sealed class CatalogServiceTests
{
    [Fact]
    public void SearchWords_SplitsQueryOnAnyWhitespace()
    {
        Assert.Equal(["austen", "pride"], CatalogService.SearchWords("  austen \t pride "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SearchWords_IsEmpty_WhenThereIsNoQuery(string? text)
    {
        Assert.Empty(CatalogService.SearchWords(text));
    }

    [Fact]
    public void SearchWords_KeepsOnlyFirstFiveWords()
    {
        var words = CatalogService.SearchWords("one two three four five six seven");

        Assert.Equal(["one", "two", "three", "four", "five"], words);
    }

    [Theory]
    [InlineData("austen", "austen")]
    [InlineData("100%", @"100\%")]
    [InlineData("snake_case", @"snake\_case")]
    [InlineData(@"back\slash", @"back\\slash")]
    public void EscapeLike_TreatsWildcardsAsPlainCharacters(string value, string expected)
    {
        Assert.Equal(expected, CatalogService.EscapeLike(value));
    }
}
