using YarPhoenix.Catalog.Application;

namespace YarPhoenix.Catalog.Infrastructure.OpenLibrary;

internal static class OpenLibraryQuery
{
    // Ключи жанров каталога → subject-термины Open Library.
    private static readonly Dictionary<string, string> GenreSubjects = new()
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

    public static string Build(BookSearchQuery query)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            parts.Add(query.Text.Trim());
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
}
