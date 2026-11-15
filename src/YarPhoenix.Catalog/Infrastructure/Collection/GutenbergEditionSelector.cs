using System.Text;
using System.Text.RegularExpressions;

namespace YarPhoenix.Catalog.Infrastructure.Collection;

// У одной работы Open Library бывает десяток номеров Gutenberg: переводы, аудио, отдельные тома,
// переиздания. Для чтения нужно одно издание — английский текст и по возможности книга целиком.
internal static partial class GutenbergEditionSelector
{
    private const string English = "en";

    public static GutenbergEdition? Pick(string workTitle, IEnumerable<int> ids, GutenbergCatalog catalog) =>
        ids
            .Select(catalog.Find)
            .OfType<GutenbergEdition>()
            // Строго "en": издание «en; fr» двуязычное, читать его как английский текст нельзя.
            .Where(edition => string.Equals(edition.Language, English, StringComparison.Ordinal))
            .OrderBy(edition => Rank(workTitle, edition.Title))
            .ThenBy(edition => edition.Id)
            .FirstOrDefault();

    // 0 — заголовок издания совпадает с названием работы; 1 — другое целое издание;
    // 2 — отдельный том или часть.
    internal static int Rank(string workTitle, string editionTitle)
    {
        if (IsPartial(editionTitle))
        {
            return 2;
        }

        var work = Normalize(workTitle);
        return Normalize(editionTitle) == work || Normalize(MainTitle(editionTitle)) == work ? 0 : 1;
    }

    internal static bool IsPartial(string editionTitle) => PartRegex().IsMatch(editionTitle);

    // «Frankenstein; Or, The Modern Prometheus» → «Frankenstein»: подзаголовок отделён
    // точкой с запятой, двоеточием или переводом строки.
    private static string MainTitle(string title)
    {
        var end = title.IndexOfAny([';', ':', '\n']);
        return end < 0 ? title : title[..end];
    }

    private static string Normalize(string title)
    {
        var builder = new StringBuilder(title.Length);
        foreach (var character in title.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (builder.Length > 0 && builder[^1] != ' ')
            {
                builder.Append(' ');
            }
        }

        var normalized = builder.ToString().Trim();
        return normalized.StartsWith("the ", StringComparison.Ordinal) ? normalized[4..] : normalized;
    }

    // «Volume 1», «Vol. II», «Part 2», «Book 03».
    [GeneratedRegex(@"\b(volume|vol\.?|part|book)\s+(\d+|[ivxlc]+)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PartRegex();
}
