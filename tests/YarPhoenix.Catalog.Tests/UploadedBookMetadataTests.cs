using YarPhoenix.Catalog.Domain;
using YarPhoenix.Catalog.Infrastructure.Epub;

namespace YarPhoenix.Catalog.Tests;

// K1: загруженный пользователем EPUB не должен попадать в общую каталожную запись.
public sealed class UploadedBookMetadataTests
{
    private static Book CatalogRecord() => new()
    {
        Id = Guid.NewGuid(),
        Provider = BookProvider.OpenLibrary,
        ExternalId = "OL66554W",
        Title = "Pride and Prejudice",
        Author = "Jane Austen",
        CoverId = 14348537,
        Description = "A novel of manners.",
        FirstPublishYear = 1813,
        Subjects = ["Fiction"],
        InCollection = true,
    };

    private static Book Uploaded() => new()
    {
        Id = Guid.NewGuid(),
        FileId = "user-file.epub",
        ReadableSource = ReadableSource.Upload,
        Title = "Pride and Prejudice",
        Author = "Jane Austen",
    };

    [Fact]
    public void CopyCatalogMetadata_LeavesTheFileWithTheUploadedBook()
    {
        var uploaded = Uploaded();
        var catalogRecord = CatalogRecord();

        EpubProcessingWorker.CopyCatalogMetadata(uploaded, catalogRecord);

        Assert.Null(catalogRecord.FileId);
        Assert.Equal("user-file.epub", uploaded.FileId);
        Assert.Equal(ReadableSource.Upload, uploaded.ReadableSource);
        // Личная запись не становится каталожной и не попадает на общую витрину.
        Assert.Null(uploaded.Provider);
        Assert.Null(uploaded.ExternalId);
        Assert.False(uploaded.InCollection);
    }

    [Fact]
    public void CopyCatalogMetadata_FillsWhatTheFileDoesNotCarry()
    {
        var uploaded = Uploaded();

        EpubProcessingWorker.CopyCatalogMetadata(uploaded, CatalogRecord());

        Assert.Equal(14348537, uploaded.CoverId);
        Assert.Equal("A novel of manners.", uploaded.Description);
        Assert.Equal(1813, uploaded.FirstPublishYear);
        Assert.Equal(["Fiction"], uploaded.Subjects);
    }

    [Fact]
    public void CopyCatalogMetadata_KeepsMetadataTheUploadedBookAlreadyHas()
    {
        var uploaded = Uploaded();
        uploaded.Description = "From the publisher.";
        uploaded.FirstPublishYear = 2010;

        EpubProcessingWorker.CopyCatalogMetadata(uploaded, CatalogRecord());

        Assert.Equal("From the publisher.", uploaded.Description);
        Assert.Equal(2010, uploaded.FirstPublishYear);
    }
}
