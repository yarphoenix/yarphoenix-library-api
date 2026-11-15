using System.Text;
using System.Text.RegularExpressions;
using YarPhoenix.Catalog.Application;

namespace YarPhoenix.Catalog.Infrastructure.OpenLibrary;

internal static partial class OpenLibraryQuery
{
    // Ключи жанров каталога → subject-термины Open Library.
    internal static readonly Dictionary<string, string> GenreSubjects = new()
    {
        ["scifi"] = "\"science fiction\"",
        ["fantasy"] = "fantasy",
        ["fiction"] = "fiction",
        ["classic"] = "classics",
        ["thriller"] = "thriller",
        ["mystery"] = "mystery",
        ["mythology"] = "mythology",
        ["history"] = "history",
        ["memoir"] = "memoir",
        ["selfdev"] = "\"self-help\"",
        ["romance"] = "romance",
    };

    private static readonly Dictionary<string, string> Languages = new()
    {
        ["ru"] = "rus",
        ["en"] = "eng",
    };

    // Символы с особым смыслом в запросе Solr (Lucene).
    private const string SpecialCharacters = "+-&|!(){}[]^\"~*?:\\/";

    public static string Build(BookSearchQuery query)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            parts.Add(EscapeUserText(query.Text.Trim()));
        }

        if (query.Genre is not null && GenreSubjects.TryGetValue(query.Genre, out var subject))
        {
            parts.Add($"subject:{subject}");
        }

        if (query.Language is not null && Languages.TryGetValue(query.Language, out var language))
        {
            parts.Add($"language:{language}");
        }

        // Пустой запрос — дефолтная витрина каталога, а не пустая страница.
        if (parts.Count == 0 || (parts.Count == 1 && parts[0].StartsWith("language:", StringComparison.Ordinal)))
        {
            parts.Insert(0, "subject:fiction");
        }

        return string.Join(" AND ", parts);
    }

    // Текст из строки поиска — не язык запросов. Без экранирования «C++», «1984: роман» или
    // незакрытая кавычка дают ошибку Open Library либо неожиданную выборку, а слова AND/OR/NOT
    // работают как операторы.
    internal static string EscapeUserText(string text)
    {
        var builder = new StringBuilder(text.Length + 8);
        foreach (var character in text)
        {
            if (SpecialCharacters.Contains(character))
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return OperatorRegex().Replace(builder.ToString(), match => match.Value.ToLowerInvariant());
    }

    [GeneratedRegex(@"\b(AND|OR|NOT)\b")]
    private static partial Regex OperatorRegex();
}
