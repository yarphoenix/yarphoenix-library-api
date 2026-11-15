using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Infrastructure.OpenLibrary;

namespace YarPhoenix.Catalog.Tests;

public sealed class OpenLibraryQueryTests
{
    [Fact]
    public void Build_CombinesTextGenreAndLanguage()
    {
        var query = new BookSearchQuery(1, 24, "war and peace", "classic", "en");

        var result = OpenLibraryQuery.Build(query);

        Assert.Equal("war and peace AND subject:classics AND language:eng", result);
    }

    [Fact]
    public void Build_FallsBackToDefaultSubject_WhenQueryIsEmpty()
    {
        var query = new BookSearchQuery(1, 24, null, null, null);

        var result = OpenLibraryQuery.Build(query);

        Assert.Equal("subject:fiction", result);
    }

    [Fact]
    public void Build_AddsDefaultSubject_WhenOnlyLanguageIsSet()
    {
        var query = new BookSearchQuery(1, 24, null, null, "ru");

        var result = OpenLibraryQuery.Build(query);

        Assert.Equal("subject:fiction AND language:rus", result);
    }

    [Fact]
    public void Build_IgnoresUnknownGenreAndLanguage()
    {
        var query = new BookSearchQuery(1, 24, "dune", "unknown-genre", "de");

        var result = OpenLibraryQuery.Build(query);

        Assert.Equal("dune", result);
    }

    [Theory]
    [InlineData("C++", @"C\+\+")]
    [InlineData("1984: a novel", @"1984\: a novel")]
    [InlineData("\"unclosed quote", "\\\"unclosed quote")]
    [InlineData("self-help", @"self\-help")]
    [InlineData("war AND peace OR love", "war and peace or love")]
    public void Build_EscapesUserText(string text, string expected)
    {
        var query = new BookSearchQuery(1, 24, text, null, null);

        var result = OpenLibraryQuery.Build(query);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Build_QuotesMultiWordSubjects()
    {
        var query = new BookSearchQuery(1, 24, null, "scifi", null);

        var result = OpenLibraryQuery.Build(query);

        Assert.Equal("subject:\"science fiction\"", result);
    }
}
