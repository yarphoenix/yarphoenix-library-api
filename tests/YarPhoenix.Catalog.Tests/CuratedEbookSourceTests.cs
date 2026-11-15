using YarPhoenix.Catalog.Domain;
using YarPhoenix.Catalog.Infrastructure.Collection;

namespace YarPhoenix.Catalog.Tests;

public sealed class CuratedEbookSourceTests
{
    private static readonly CollectionOptions Options = new();

    [Fact]
    public void BuildUrls_PointsAtStandardEbooksDownload_NotTheInterstitialPage()
    {
        var urls = CuratedEbookSource.BuildUrls(
            ReadableSource.StandardEbooks, "jane-austen/pride-and-prejudice", Options);

        var url = Assert.Single(urls);
        Assert.Equal(
            "https://standardebooks.org/ebooks/jane-austen/pride-and-prejudice/downloads/"
            + "jane-austen_pride-and-prejudice.epub?source=download",
            url.AbsoluteUri);
    }

    [Fact]
    public void BuildUrls_PutsTranslatorIntoStandardEbooksFileName()
    {
        var urls = CuratedEbookSource.BuildUrls(
            ReadableSource.StandardEbooks, "leo-tolstoy/war-and-peace/louise-maude_aylmer-maude", Options);

        Assert.EndsWith(
            "/downloads/leo-tolstoy_war-and-peace_louise-maude_aylmer-maude.epub?source=download",
            Assert.Single(urls).AbsoluteUri);
    }

    [Fact]
    public void BuildUrls_TriesIllustratedGutenbergEditionFirst_ThenPlainOne()
    {
        var urls = CuratedEbookSource.BuildUrls(ReadableSource.Gutenberg, "1342", Options);

        Assert.Equal(
            [
                "https://www.gutenberg.org/ebooks/1342.epub3.images",
                "https://www.gutenberg.org/ebooks/1342.epub.noimages",
            ],
            urls.Select(url => url.AbsoluteUri));
    }

    [Fact]
    public void BuildUrls_RespectsConfiguredMirror()
    {
        var options = new CollectionOptions { GutenbergBaseUrl = "https://mirror.example/gutenberg/" };

        var urls = CuratedEbookSource.BuildUrls(ReadableSource.Gutenberg, "84", options);

        Assert.Equal("https://mirror.example/gutenberg/ebooks/84.epub3.images", urls[0].AbsoluteUri);
    }

    [Theory]
    [InlineData(ReadableSource.None)]
    [InlineData(ReadableSource.Upload)]
    public void BuildUrls_HasNothingToDownload_ForOtherSources(ReadableSource source)
    {
        Assert.Empty(CuratedEbookSource.BuildUrls(source, "anything", Options));
    }

    [Fact]
    public void LooksLikeEpub_AcceptsZipSignature()
    {
        byte[] zip = [(byte)'P', (byte)'K', 3, 4, 20, 0, 0, 0];

        Assert.True(CuratedEbookSource.LooksLikeEpub(zip));
    }

    [Fact]
    public void LooksLikeEpub_RejectsHtmlPageAndEmptyBody()
    {
        Assert.False(CuratedEbookSource.LooksLikeEpub("<!DOCTYPE html><html>"u8));
        Assert.False(CuratedEbookSource.LooksLikeEpub([]));
    }
}
