namespace YarPhoenix.Catalog.Infrastructure.OpenLibrary;

// Темы Open Library пишут и люди, и импортёры: рядом с «Fiction» лежат машинные теги
// («series:Harry_Potter», «nyt:hardcover-fiction=2011-01-01») и служебные пометки
// («Accessible book»). Здесь они отсеиваются, а оставшиеся раскладываются по жанрам каталога.
internal static class OpenLibrarySubjects
{
    // Сколько тем хранится у книги коллекции и показывается на её странице.
    public const int MaxStored = 8;

    private const int MaxLength = 60;

    private static readonly HashSet<string> ServiceTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accessible book",
        "Protected DAISY",
        "In library",
        "Lending library",
        "Internet Archive Wishlist",
        "Large type books",
        "Open Library Staff Picks",
        "OverDrive",
        "Long Now Manual for Civilization",
        "General",
    };

    // Слова, по которым тема относится к жанру, сверх subject-термина из OpenLibraryQuery:
    // Open Library называет один и тот же жанр по-разному.
    private static readonly Dictionary<string, string[]> ExtraGenreTerms = new()
    {
        ["classic"] = ["classic"],
        ["thriller"] = ["suspense"],
        ["mystery"] = ["detective"],
        ["memoir"] = ["autobiography"],
        ["romance"] = ["love stories"],
    };

    // Широкие жанры: одной темы достаточно, порог по числу совпадений к ним не применяется.
    private static readonly HashSet<string> BroadGenres = ["fiction", "classic"];

    // На каждые столько тем у работы нужно ещё одно совпадение, чтобы признать жанр.
    private const int SubjectsPerExtraMatch = 50;

    public static List<string> Clean(IEnumerable<string>? subjects, int limit = MaxStored)
    {
        var result = new List<string>();
        if (subjects is null)
        {
            return result;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in subjects)
        {
            var subject = raw?.Trim();
            if (string.IsNullOrEmpty(subject)
                || subject.Length > MaxLength
                || IsMachineTag(subject)
                || ServiceTags.Contains(subject)
                || !seen.Add(subject))
            {
                continue;
            }

            result.Add(subject);
            if (result.Count == limit)
            {
                break;
            }
        }

        return result;
    }

    // Жанры считаются по всем темам работы, а не по тем нескольким, что остаются для показа.
    public static string[] ToGenres(IEnumerable<string>? subjects)
    {
        if (subjects is null)
        {
            return [];
        }

        var normalized = subjects
            .Where(subject => !string.IsNullOrWhiteSpace(subject) && !IsMachineTag(subject))
            .Select(subject => subject.ToLowerInvariant())
            // «History and criticism» — пометка литературоведения, а не исторический жанр.
            .Where(subject => !subject.Contains("criticism", StringComparison.Ordinal))
            .ToList();

        // У известных книг в Open Library по сотне с лишним тем, и одиночный случайный тег ещё не
        // жанр: у «Алисы в Стране чудес» из 171 темы одна — «science fiction» и восемь — про
        // фэнтези. Чем больше тем, тем больше совпадений нужно.
        var required = 1 + (normalized.Count / SubjectsPerExtraMatch);

        return OpenLibraryQuery.GenreSubjects
            .Where(genre =>
            {
                var terms = TermsFor(genre.Key, genre.Value).ToList();
                var matches = normalized.Count(subject => terms.Any(term => ContainsWord(subject, term)));
                return matches >= (BroadGenres.Contains(genre.Key) ? 1 : required);
            })
            .Select(genre => genre.Key)
            .ToArray();
    }

    // «ключ:значение» без пробела после двоеточия — запись импортёра, а не тема.
    internal static bool IsMachineTag(string subject)
    {
        var colon = subject.IndexOf(':');
        if (colon <= 0 || colon == subject.Length - 1 || char.IsWhiteSpace(subject[colon + 1]))
        {
            return false;
        }

        return !subject.AsSpan(0, colon).Contains(' ');
    }

    private static IEnumerable<string> TermsFor(string genre, string querySubject)
    {
        yield return querySubject.Trim('"');

        if (ExtraGenreTerms.TryGetValue(genre, out var extra))
        {
            foreach (var term in extra)
            {
                yield return term;
            }
        }
    }

    // Совпадение с начала слова: «fiction» находится в «science fiction», но не в «nonfiction».
    private static bool ContainsWord(string subject, string term)
    {
        var index = subject.IndexOf(term, StringComparison.Ordinal);
        while (index >= 0)
        {
            if (index == 0 || !char.IsLetter(subject[index - 1]))
            {
                return true;
            }

            index = subject.IndexOf(term, index + 1, StringComparison.Ordinal);
        }

        return false;
    }
}
