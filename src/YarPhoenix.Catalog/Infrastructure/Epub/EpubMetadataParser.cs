using System.IO.Compression;
using System.Xml.Linq;

namespace YarPhoenix.Catalog.Infrastructure.Epub;

internal sealed record EpubMetadata(string? Title, string? Author, string? Isbn);

// Читает метаданные из OPF внутри EPUB (ТЗ §5.4): container.xml → rootfile → dc:*.
internal static class EpubMetadataParser
{
    private static readonly XNamespace ContainerNs = "urn:oasis:names:tc:opendocument:xmlns:container";
    private static readonly XNamespace DcNs = "http://purl.org/dc/elements/1.1/";

    public static EpubMetadata? Parse(Stream epubStream)
    {
        try
        {
            using var archive = new ZipArchive(epubStream, ZipArchiveMode.Read, leaveOpen: true);

            var container = archive.GetEntry("META-INF/container.xml");
            if (container is null)
            {
                return null;
            }

            string? opfPath;
            using (var stream = container.Open())
            {
                opfPath = XDocument.Load(stream)
                    .Descendants(ContainerNs + "rootfile")
                    .FirstOrDefault()?
                    .Attribute("full-path")?.Value;
            }

            if (opfPath is null || archive.GetEntry(opfPath) is not { } opfEntry)
            {
                return null;
            }

            using var opfStream = opfEntry.Open();
            var opf = XDocument.Load(opfStream);

            var title = opf.Descendants(DcNs + "title").FirstOrDefault()?.Value.Trim();
            var author = opf.Descendants(DcNs + "creator").FirstOrDefault()?.Value.Trim();
            var isbn = opf.Descendants(DcNs + "identifier")
                .Select(e => NormalizeIsbn(e.Value))
                .FirstOrDefault(v => v is not null);

            return new EpubMetadata(title, author, isbn);
        }
        catch (InvalidDataException)
        {
            // Битый zip — метаданных нет, книга остаётся с именем файла.
            return null;
        }
    }

    // ISBN в EPUB встречается с префиксами (urn:isbn:...) и дефисами — нормализуем.
    internal static string? NormalizeIsbn(string raw)
    {
        var cleaned = new string(raw.Where(c => char.IsAsciiDigit(c) || c is 'X' or 'x').ToArray());
        return cleaned.Length is 10 or 13 ? cleaned.ToUpperInvariant() : null;
    }
}
