using System.IO.Compression;
using System.Text;
using YarPhoenix.Catalog.Infrastructure.Epub;

namespace YarPhoenix.Catalog.Tests;

public sealed class EpubMetadataParserTests
{
    [Fact]
    public void Parse_ExtractsTitleAuthorAndIsbn_FromOpf()
    {
        using var epub = BuildEpub(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="uid">
              <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                <dc:identifier id="uid">urn:isbn:978-0-14-044793-4</dc:identifier>
                <dc:title>War and Peace</dc:title>
                <dc:creator>Leo Tolstoy</dc:creator>
              </metadata>
            </package>
            """);

        var metadata = EpubMetadataParser.Parse(epub);

        Assert.NotNull(metadata);
        Assert.Equal("War and Peace", metadata.Title);
        Assert.Equal("Leo Tolstoy", metadata.Author);
        Assert.Equal("9780140447934", metadata.Isbn);
    }

    [Fact]
    public void Parse_ReturnsNull_ForNonZipContent()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not a zip at all"));

        Assert.Null(EpubMetadataParser.Parse(stream));
    }

    [Fact]
    public void Parse_ReturnsMetadataWithNulls_WhenFieldsMissing()
    {
        using var epub = BuildEpub(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="3.0">
              <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                <dc:identifier>internal-id-not-isbn</dc:identifier>
              </metadata>
            </package>
            """);

        var metadata = EpubMetadataParser.Parse(epub);

        Assert.NotNull(metadata);
        Assert.Null(metadata.Title);
        Assert.Null(metadata.Author);
        Assert.Null(metadata.Isbn);
    }

    [Theory]
    [InlineData("urn:isbn:978-0-14-044793-4", "9780140447934")]
    [InlineData("0-14-044793-X", "014044793X")]
    [InlineData("not an isbn", null)]
    [InlineData("12345", null)]
    public void NormalizeIsbn_HandlesPrefixesAndGarbage(string raw, string? expected)
    {
        Assert.Equal(expected, EpubMetadataParser.NormalizeIsbn(raw));
    }

    private static MemoryStream BuildEpub(string opfContent)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "mimetype", "application/epub+zip");
            WriteEntry(
                archive,
                "META-INF/container.xml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                  <rootfiles>
                    <rootfile full-path="content.opf" media-type="application/oebps-package+xml"/>
                  </rootfiles>
                </container>
                """);
            WriteEntry(archive, "content.opf", opfContent);
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        using var entryStream = archive.CreateEntry(name).Open();
        entryStream.Write(Encoding.UTF8.GetBytes(content));
    }
}
