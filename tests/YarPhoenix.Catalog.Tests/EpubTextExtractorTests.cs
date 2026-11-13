using System.IO.Compression;
using System.Text;
using YarPhoenix.Ai.Infrastructure.Epub;

namespace YarPhoenix.Catalog.Tests;

public sealed class EpubTextExtractorTests
{
    [Fact]
    public void Extract_ReturnsChaptersInSpineOrder()
    {
        using var epub = BuildEpub();

        var chapters = EpubTextExtractor.Extract(epub);

        Assert.Equal(2, chapters.Count);
        Assert.Contains("First chapter body", chapters[0]);
        Assert.Contains("Second chapter body", chapters[1]);
    }

    [Fact]
    public void Extract_StripsTagsAndDecodesEntities()
    {
        using var epub = BuildEpub();

        var chapters = EpubTextExtractor.Extract(epub);

        Assert.DoesNotContain("<p>", chapters[0]);
        Assert.Contains("Tom & Jerry", chapters[0]);
    }

    [Fact]
    public void Extract_ReturnsEmpty_ForCorruptZip()
    {
        using var garbage = new MemoryStream("not a zip"u8.ToArray());
        Assert.Empty(EpubTextExtractor.Extract(garbage));
    }

    private static MemoryStream BuildEpub()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "META-INF/container.xml",
                """
                <?xml version="1.0"?>
                <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                  <rootfiles>
                    <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
                  </rootfiles>
                </container>
                """);

            AddEntry(archive, "OEBPS/content.opf",
                """
                <?xml version="1.0"?>
                <package xmlns="http://www.idpf.org/2007/opf" version="3.0">
                  <manifest>
                    <item id="c1" href="ch1.xhtml" media-type="application/xhtml+xml"/>
                    <item id="c2" href="ch2.xhtml" media-type="application/xhtml+xml"/>
                  </manifest>
                  <spine>
                    <itemref idref="c1"/>
                    <itemref idref="c2"/>
                  </spine>
                </package>
                """);

            AddEntry(archive, "OEBPS/ch1.xhtml",
                "<html><body><p>First chapter body. Tom &amp; Jerry.</p></body></html>");
            AddEntry(archive, "OEBPS/ch2.xhtml",
                "<html><body><p>Second chapter body.</p></body></html>");
        }

        stream.Position = 0;
        return stream;
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
