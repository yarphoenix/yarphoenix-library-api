using System.Text.RegularExpressions;

namespace YarPhoenix.Catalog.Infrastructure.OpenLibrary;

// Описания Open Library пишут люди в вики-разметке: Markdown-ссылки, сноски, блок «см. также»
// после черты. На странице книги это выглядит мусором — оставляем только сам текст.
internal static partial class OpenLibraryText
{
    public static string? CleanDescription(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Replace("\r\n", "\n").Replace('\r', '\n');

        // Всё после черты — служебные списки («Also contained in», «See also»), а не описание.
        var rule = RuleRegex().Match(text);
        if (rule.Success)
        {
            text = text[..rule.Index];
        }

        text = ReferenceDefinitionRegex().Replace(text, string.Empty);
        text = SourceNoteRegex().Replace(text, string.Empty);
        text = ReferenceLinkRegex().Replace(text, "$1");
        text = InlineLinkRegex().Replace(text, "$1");
        text = BoldRegex().Replace(text, "$1");
        text = TrailingSpacesRegex().Replace(text, "\n");
        text = BlankLinesRegex().Replace(text, "\n\n").Trim();

        return text.Length == 0 ? null : text;
    }

    // "----------" отдельной строкой.
    [GeneratedRegex(@"^[ \t]*-{5,}[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex RuleRegex();

    // "[1]: https://example.org/…" — определение сноски.
    [GeneratedRegex(@"^[ \t]*\[\d+\]:[ \t]*\S.*$", RegexOptions.Multiline)]
    private static partial Regex ReferenceDefinitionRegex();

    // "([source][1])" — пометка об источнике в конце абзаца.
    [GeneratedRegex(@"[ \t]*\(\[[^\]]*\]\[\d+\]\)")]
    private static partial Regex SourceNoteRegex();

    // "[text][1]" → text.
    [GeneratedRegex(@"\[([^\]]+)\]\[\d+\]")]
    private static partial Regex ReferenceLinkRegex();

    // "[text](https://…)" → text.
    [GeneratedRegex(@"\[([^\]]+)\]\(https?://[^)\s]+\)")]
    private static partial Regex InlineLinkRegex();

    // "**text**" → text.
    [GeneratedRegex(@"\*\*(.+?)\*\*", RegexOptions.Singleline)]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"[ \t]+\n")]
    private static partial Regex TrailingSpacesRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLinesRegex();
}
