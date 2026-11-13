using System.Text.Json;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Infrastructure.InternetArchive;

namespace YarPhoenix.Catalog.Tests;

public sealed class InternetArchiveTests
{
    [Fact]
    public void LendingFilter_RejectsAccessRestrictedItem()
    {
        var metadata = new IaMetadataResponse
        {
            Files = [],
            Metadata = new IaMetadata { AccessRestrictedItem = "true" },
        };

        Assert.True(IaLendingFilter.IsRestricted(metadata));
    }

    [Fact]
    public void LendingFilter_RejectsInLibraryCollection()
    {
        var metadata = new IaMetadataResponse
        {
            Files = [],
            Metadata = new IaMetadata
            {
                Collection = JsonElement("[\"internetarchivebooks\",\"inlibrary\",\"printdisabled\"]"),
            },
        };

        Assert.True(IaLendingFilter.IsRestricted(metadata));
    }

    [Fact]
    public void LendingFilter_AllowsOpenItem()
    {
        var metadata = new IaMetadataResponse
        {
            Files = [],
            Metadata = new IaMetadata
            {
                Collection = JsonElement("[\"university_of_illinois_urbana-champaign\",\"americana\"]"),
            },
        };

        Assert.False(IaLendingFilter.IsRestricted(metadata));
    }

    [Fact]
    public void LendingFilter_HandlesScalarCollection()
    {
        var metadata = new IaMetadataResponse
        {
            Files = [],
            Metadata = new IaMetadata { Collection = JsonElement("\"inlibrary\"") },
        };

        Assert.True(IaLendingFilter.IsRestricted(metadata));
    }

    [Fact]
    public void LendingFilter_RejectsMissingMetadata()
    {
        Assert.True(IaLendingFilter.IsRestricted(new IaMetadataResponse()));
    }

    [Fact]
    public void FileSelector_PicksOpenEpub()
    {
        var files = new List<IaFile>
        {
            new() { Name = "book_djvu.txt", Format = "DjVuTXT" },
            new() { Name = "book.epub", Format = "EPUB", Size = "1301467" },
        };

        var picked = IaFileSelector.PickOpenEpub(files);

        Assert.NotNull(picked);
        Assert.Equal("book.epub", picked.Name);
    }

    [Fact]
    public void FileSelector_SkipsPrivateAndLcpEpubs()
    {
        var files = new List<IaFile>
        {
            new() { Name = "book_lcp.epub", Format = "EPUB" },
            new() { Name = "restricted.epub", Format = "EPUB", Private = "true" },
        };

        Assert.Null(IaFileSelector.PickOpenEpub(files));
    }

    [Theory]
    [InlineData("Лев Николаевич Толстой", true)]
    [InlineData("Leo Tolstoy", true)]
    [InlineData("Tolstoy, Leo, graf, 1828-1910", true)]
    [InlineData("Fyodor Dostoevsky", true)]
    [InlineData("Jane Austen", true)]
    [InlineData("Arthur Conan Doyle", true)]
    [InlineData("Stephen King", false)]
    [InlineData("J. K. Rowling", false)]
    [InlineData("", false)]
    public void Whitelist_AllowsOnlyKnownClassics(string author, bool expected)
    {
        Assert.Equal(expected, PublicDomainWhitelist.IsAllowed(author));
    }

    private static JsonElement JsonElement(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();
}
