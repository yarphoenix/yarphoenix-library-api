using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
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
    public void BuildUrls_TriesIllustratedGutenbergEdition_OnSiteThenMirror_BeforePlainOne()
    {
        var urls = CuratedEbookSource.BuildUrls(ReadableSource.Gutenberg, "1342", Options);

        Assert.Equal(
            [
                "https://www.gutenberg.org/ebooks/1342.epub3.images",
                "http://aleph.gutenberg.org/cache/epub/1342/pg1342-images-3.epub",
                "https://www.gutenberg.org/ebooks/1342.epub.noimages",
                "http://aleph.gutenberg.org/cache/epub/1342/pg1342.epub",
            ],
            urls.Select(url => url.AbsoluteUri));
    }

    [Fact]
    public void BuildUrls_RespectsConfiguredAddresses()
    {
        var options = new CollectionOptions
        {
            GutenbergBaseUrl = "https://main.example/gutenberg/",
            GutenbergMirrorBaseUrl = "https://mirror.example/",
        };

        var urls = CuratedEbookSource.BuildUrls(ReadableSource.Gutenberg, "84", options);

        Assert.Equal("https://main.example/gutenberg/ebooks/84.epub3.images", urls[0].AbsoluteUri);
        Assert.Equal("https://mirror.example/cache/epub/84/pg84-images-3.epub", urls[1].AbsoluteUri);
    }

    [Fact]
    public void BuildUrls_SkipsMirror_WhenItIsSwitchedOff()
    {
        var options = new CollectionOptions { GutenbergMirrorBaseUrl = string.Empty };

        var urls = CuratedEbookSource.BuildUrls(ReadableSource.Gutenberg, "84", options);

        Assert.Equal(
            [
                "https://www.gutenberg.org/ebooks/84.epub3.images",
                "https://www.gutenberg.org/ebooks/84.epub.noimages",
            ],
            urls.Select(url => url.AbsoluteUri));
    }

    [Fact]
    public async Task DownloadAsync_TakesFileFromMirror_WhenMainSiteRefuses()
    {
        // Так отвечает основной сайт, когда недоступен целиком: 403 на любой адрес.
        var handler = new StubHandler(request => request.RequestUri!.Host == "aleph.gutenberg.org"
            ? Epub()
            : new HttpResponseMessage(HttpStatusCode.Forbidden));
        var source = CreateSource(handler);

        await using var file = await source.DownloadAsync(
            ReadableSource.Gutenberg, "11", CancellationToken.None);

        Assert.NotNull(file);
        Assert.Equal(
            [
                "https://www.gutenberg.org/ebooks/11.epub3.images",
                "http://aleph.gutenberg.org/cache/epub/11/pg11-images-3.epub",
            ],
            handler.Requested);
    }

    [Fact]
    public async Task DownloadAsync_TriesNextAddress_WhenOneFailsOnTheNetwork()
    {
        var handler = new StubHandler(request => request.RequestUri!.Host == "aleph.gutenberg.org"
            ? Epub()
            : throw new HttpRequestException("connection refused"));
        var source = CreateSource(handler);

        await using var file = await source.DownloadAsync(
            ReadableSource.Gutenberg, "11", CancellationToken.None);

        Assert.NotNull(file);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsNothing_WhenNoAddressHasTheFile()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var source = CreateSource(handler);

        var file = await source.DownloadAsync(
            ReadableSource.Gutenberg, "11", CancellationToken.None);

        Assert.Null(file);
        Assert.Equal(4, handler.Requested.Count);
    }

    [Fact]
    public async Task DownloadCatalogFeedAsync_FallsBackToMirror_WhenMainSiteRefuses()
    {
        var handler = new StubHandler(request => request.RequestUri!.Host == "aleph.gutenberg.org"
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Text#,Type\n11,Text\n") }
            : new HttpResponseMessage(HttpStatusCode.Forbidden));
        var source = CreateSource(handler);

        await using var feed = await source.DownloadCatalogFeedAsync(CancellationToken.None);

        Assert.True(feed.Length > 0);
        Assert.Equal(
            "http://aleph.gutenberg.org/cache/epub/feeds/pg_catalog.csv",
            handler.Requested[^1]);
    }

    [Fact]
    public async Task DownloadCatalogFeedAsync_Fails_WhenEveryAddressRefuses()
    {
        var source = CreateSource(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => source.DownloadCatalogFeedAsync(CancellationToken.None));
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

    private static CuratedEbookSource CreateSource(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(Options),
            NullLogger<CuratedEbookSource>.Instance);

    // Тело с сигнатурой ZIP — этого достаточно, чтобы источник принял ответ за EPUB.
    private static HttpResponseMessage Epub() =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent([(byte)'P', (byte)'K', 3, 4, 20, 0, 0, 0]) };

    // Отвечает на запросы заданной функцией и запоминает адреса в порядке обращения.
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Requested { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requested.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(respond(request));
        }
    }
}
