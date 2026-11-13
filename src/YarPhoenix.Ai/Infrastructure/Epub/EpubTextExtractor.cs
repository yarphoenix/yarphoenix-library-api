using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace YarPhoenix.Ai.Infrastructure.Epub;

// Извлекает текст EPUB по главам в порядке spine (ТЗ §7.2): нужен серверу, чтобы
// обрезать контекст ассистента по позиции чтения (CFI). Свой парсер, а не из Catalog:
// там internal и заточен под метаданные OPF, здесь нужен текст документов spine.
public static partial class EpubTextExtractor
{
    private static readonly XNamespace ContainerNs = "urn:oasis:names:tc:opendocument:xmlns:container";

    // Главы в порядке spine; индекс совпадает с индексом spine-itemref в CFI (/6/2N → N-1).
    public static IReadOnlyList<string> Extract(Stream epubStream)
    {
        try
        {
            using var archive = new ZipArchive(epubStream, ZipArchiveMode.Read, leaveOpen: true);

            var opfPath = ReadOpfPath(archive);
            if (opfPath is null || archive.GetEntry(opfPath) is not { } opfEntry)
            {
                return [];
            }

            XDocument opf;
            using (var opfStream = opfEntry.Open())
            {
                opf = XDocument.Load(opfStream);
            }

            var manifest = opf.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "manifest");
            var spine = opf.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "spine");
            if (manifest is null || spine is null)
            {
                return [];
            }

            var hrefById = manifest.Elements()
                .Where(e => e.Name.LocalName == "item")
                .Where(e => e.Attribute("id") is not null && e.Attribute("href") is not null)
                .ToDictionary(e => e.Attribute("id")!.Value, e => e.Attribute("href")!.Value);

            var opfDir = GetDirectory(opfPath);
            var chapters = new List<string>();

            foreach (var itemref in spine.Elements().Where(e => e.Name.LocalName == "itemref"))
            {
                var idref = itemref.Attribute("idref")?.Value;
                if (idref is null || !hrefById.TryGetValue(idref, out var href))
                {
                    continue;
                }

                var entry = ResolveEntry(archive, opfDir, href);
                if (entry is null)
                {
                    // Пропущенная глава осталась бы пустой строкой — держим индексы
                    // spine выровненными с CFI, добавляя заглушку.
                    chapters.Add(string.Empty);
                    continue;
                }

                using var reader = new StreamReader(entry.Open());
                chapters.Add(HtmlToText(reader.ReadToEnd()));
            }

            return chapters;
        }
        catch (InvalidDataException)
        {
            return [];
        }
    }

    private static string? ReadOpfPath(ZipArchive archive)
    {
        var container = archive.GetEntry("META-INF/container.xml");
        if (container is null)
        {
            return null;
        }

        using var stream = container.Open();
        return XDocument.Load(stream)
            .Descendants(ContainerNs + "rootfile")
            .FirstOrDefault()?
            .Attribute("full-path")?.Value;
    }

    // href в OPF относителен к папке OPF; резолвим и ищем запись без учёта регистра.
    private static ZipArchiveEntry? ResolveEntry(ZipArchive archive, string opfDir, string href)
    {
        var clean = WebUtility.UrlDecode(href.Split('#')[0]);
        var combined = string.IsNullOrEmpty(opfDir) ? clean : $"{opfDir}/{clean}";
        var normalized = NormalizePath(combined);

        return archive.GetEntry(normalized)
            ?? archive.Entries.FirstOrDefault(e =>
                string.Equals(e.FullName, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetDirectory(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash < 0 ? string.Empty : path[..slash];
    }

    // Схлопывает "./" и "../" в путях внутри архива.
    private static string NormalizePath(string path)
    {
        var parts = new List<string>();
        foreach (var segment in path.Split('/'))
        {
            if (segment is "" or ".")
            {
                continue;
            }

            if (segment == ".." && parts.Count > 0)
            {
                parts.RemoveAt(parts.Count - 1);
            }
            else if (segment != "..")
            {
                parts.Add(segment);
            }
        }

        return string.Join('/', parts);
    }

    // XHTML бывает невалидным XML — снимаем теги регулярками, надёжнее XDocument.
    private static string HtmlToText(string html)
    {
        html = ScriptStyleRegex().Replace(html, " ");
        html = TagRegex().Replace(html, " ");
        html = WebUtility.HtmlDecode(html);
        return WhitespaceRegex().Replace(html, " ").Trim();
    }

    [GeneratedRegex(@"<(script|style)[^>]*>.*?</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ScriptStyleRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
