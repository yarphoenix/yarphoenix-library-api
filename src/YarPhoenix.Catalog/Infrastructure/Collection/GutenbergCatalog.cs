using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace YarPhoenix.Catalog.Infrastructure.Collection;

internal sealed record GutenbergPerson(string Name, int? DeathYear);

internal sealed record GutenbergEdition(
    int Id,
    string Title,
    string Language,
    IReadOnlyList<GutenbergPerson> People);

// Каталог Project Gutenberg из официального фида pg_catalog.csv. Импорту коллекции он даёт то,
// чего нет в Open Library: язык и тип издания, его создателей с годами жизни (правило
// общественного достояния) и годы жизни по имени — для книг Standard Ebooks без номера Gutenberg.
internal sealed partial class GutenbergCatalog
{
    private readonly Dictionary<int, GutenbergEdition> _editions = [];
    private readonly Dictionary<string, int?> _deathYearsByName = [];

    public int Count => _editions.Count;

    public static GutenbergCatalog Parse(TextReader reader)
    {
        var catalog = new GutenbergCatalog();
        Columns? columns = null;

        foreach (var record in CsvReader.Read(reader))
        {
            if (columns is null)
            {
                columns = Columns.From(record);
                continue;
            }

            catalog.Add(record, columns);
        }

        return catalog;
    }

    public GutenbergEdition? Find(int id) => _editions.GetValueOrDefault(id);

    // Год смерти по имени в любом порядке слов: «Jane Austen» и «Austen, Jane» — один ключ.
    // false — такого человека в каталоге нет; true с null — есть, но год смерти не указан.
    public bool TryGetDeathYear(string name, out int? deathYear) =>
        _deathYearsByName.TryGetValue(NameKey(name), out deathYear);

    private void Add(List<string> record, Columns columns)
    {
        if (record.Count <= columns.Last
            || !int.TryParse(record[columns.Id], NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            // Аудиокниги, ноты и наборы данных читалка не откроет.
            || !string.Equals(record[columns.Type].Trim(), "Text", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var people = ParsePeople(record[columns.Authors]);
        _editions[id] = new GutenbergEdition(
            id,
            record[columns.Title].Trim(),
            record[columns.Language].Trim(),
            people);

        foreach (var person in people)
        {
            Remember(person);
        }
    }

    private void Remember(GutenbergPerson person)
    {
        var key = NameKey(person.Name);
        if (key.Length == 0)
        {
            return;
        }

        // Тёзки: оставляем самый поздний известный год — ошибка в эту сторону лишь отсеет книгу.
        if (!_deathYearsByName.TryGetValue(key, out var known)
            || (person.DeathYear is { } year && (known is null || year > known)))
        {
            _deathYearsByName[key] = person.DeathYear ?? known;
        }
    }

    // "Marx, Karl, 1818-1883; Engels, Friedrich, 1820-1895; Moore, Samuel, 1838-1911 [Translator]"
    internal static List<GutenbergPerson> ParsePeople(string authors)
    {
        var people = new List<GutenbergPerson>();
        var entries = authors.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var raw in entries)
        {
            // Роль в скобках для срока охраны неважна: переводчик и иллюстратор — тоже авторы.
            var entry = RoleRegex().Replace(raw, string.Empty).Trim();
            if (entry.Length == 0)
            {
                continue;
            }

            // Годы жизни — последний фрагмент после запятой, если в нём есть цифры.
            var lastComma = entry.LastIndexOf(',');
            var tail = lastComma >= 0 ? entry[(lastComma + 1)..] : string.Empty;

            people.Add(tail.Any(char.IsAsciiDigit)
                ? new GutenbergPerson(entry[..lastComma].Trim(), ParseDeathYear(tail))
                : new GutenbergPerson(entry, null));
        }

        return people;
    }

    // "1775-1817" → 1817, "1850?-1900?" → 1900, "70 BCE-19 BCE" → -19, "1860-" → null.
    internal static int? ParseDeathYear(string lifeDates)
    {
        var match = DeathYearRegex().Match(lifeDates);
        if (!match.Success)
        {
            return null;
        }

        var year = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
        if (match.Groups["era"].Success)
        {
            return -year;
        }

        // «active 1920-1940» — годы деятельности, а не жизни: для недавних дат год смерти неизвестен.
        return year >= 1800 && lifeDates.Contains("active", StringComparison.OrdinalIgnoreCase) ? null : year;
    }

    internal static string NameKey(string name)
    {
        // Уточнение в скобках — «Wells, H. G. (Herbert George)» — в ключ не входит.
        var decomposed = ParenthesesRegex().Replace(name, " ")
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetter(character) ? character : ' ');
        }

        // Инициалы отбрасываются: «H. G. Wells» и «Herbert George Wells» пишут по-разному.
        var words = builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 1)
            .Order(StringComparer.Ordinal);

        return string.Join(' ', words);
    }

    [GeneratedRegex(@"\s*\[[^\]]*\]\s*$")]
    private static partial Regex RoleRegex();

    [GeneratedRegex(@"-\s*(?<year>\d{1,4})\??\s*(?<era>BCE|BC)?\.?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DeathYearRegex();

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex ParenthesesRegex();

    private sealed record Columns(int Id, int Type, int Title, int Language, int Authors)
    {
        public int Last => Math.Max(Math.Max(Id, Type), Math.Max(Math.Max(Title, Language), Authors));

        public static Columns From(List<string> header)
        {
            return new Columns(
                IndexOf("Text#"),
                IndexOf("Type"),
                IndexOf("Title"),
                IndexOf("Language"),
                IndexOf("Authors"));

            int IndexOf(string name)
            {
                var index = header.FindIndex(column => string.Equals(column.Trim(), name, StringComparison.Ordinal));
                return index >= 0
                    ? index
                    : throw new InvalidDataException($"Project Gutenberg catalog has no '{name}' column.");
            }
        }
    }
}
