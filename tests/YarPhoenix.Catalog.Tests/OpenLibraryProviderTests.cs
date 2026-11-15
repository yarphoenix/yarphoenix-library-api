using System.Text.Json;
using YarPhoenix.Catalog.Infrastructure.OpenLibrary;

namespace YarPhoenix.Catalog.Tests;

public sealed class OpenLibraryProviderTests
{
    [Theory]
    [InlineData("/works/OL45883W", "OL45883W")]
    [InlineData("OL45883W", "OL45883W")]
    public void ExtractId_StripsWorksPrefix(string key, string expected)
    {
        Assert.Equal(expected, OpenLibraryProvider.ExtractId(key));
    }

    [Fact]
    public void ExtractDescription_ReadsPlainString()
    {
        var element = JsonDocument.Parse("\"A great novel.\"").RootElement;

        Assert.Equal("A great novel.", OpenLibraryProvider.ExtractDescription(element));
    }

    [Fact]
    public void ExtractDescription_ReadsTypedObject()
    {
        var element = JsonDocument.Parse("{\"type\":\"/type/text\",\"value\":\"A great novel.\"}").RootElement;

        Assert.Equal("A great novel.", OpenLibraryProvider.ExtractDescription(element));
    }

    [Fact]
    public void ExtractDescription_ReturnsNull_WhenMissing()
    {
        Assert.Null(OpenLibraryProvider.ExtractDescription(null));
    }

    [Theory]
    [InlineData(12707846, 12707846)]
    [InlineData(-1, null)]
    [InlineData(0, null)]
    [InlineData(null, null)]
    public void NormalizeCover_TreatsPlaceholderIdsAsMissing(int? coverId, int? expected)
    {
        Assert.Equal(expected, OpenLibraryProvider.NormalizeCover(coverId));
    }

    [Fact]
    public void ParseGutenbergIds_KeepsDistinctPositiveNumbers()
    {
        string[] ids = ["19033", "11", "19033", "not-a-number", "0", "928"];

        Assert.Equal([19033, 11, 928], OpenLibraryProvider.ParseGutenbergIds(ids));
    }

    [Theory]
    [InlineData("1869", 1869)]
    [InlineData("January 1, 1869", 1869)]
    [InlineData(null, null)]
    [InlineData("unknown", null)]
    public void ExtractYear_ParsesYearFromFreeText(string? input, int? expected)
    {
        Assert.Equal(expected, OpenLibraryProvider.ExtractYear(input));
    }
}
