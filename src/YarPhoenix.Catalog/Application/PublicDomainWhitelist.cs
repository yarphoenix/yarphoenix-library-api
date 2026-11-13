using System.Globalization;
using System.Text;

namespace YarPhoenix.Catalog.Application;

// Вайтлист авторов (ТЗ §5.1.2): PD-статус проверяем МЫ, а не доверяем тегам сайта.
// Internet Archive метит PD по праву США; в РФ срок другой (70 лет после смерти,
// +4 для участников ВОВ). MVP — только однозначная классика XIX в., чей статус
// вне сомнений в обеих юрисдикциях. Фамилии — в кириллице и латинице/транслите,
// матч по подстроке нормализованного имени автора.
public static class PublicDomainWhitelist
{
    private static readonly string[] RawSurnames =
    [
        // Русская классика XIX в. (статус PD бесспорен).
        "пушкин", "pushkin",
        "гоголь", "gogol",
        "достоевский", "dostoevsky", "dostoyevsky", "dostoevskii",
        "толстой", "tolstoy", "tolstoi",
        "чехов", "chekhov", "chekov",
        "тургенев", "turgenev",
        "лермонтов", "lermontov",
        "гончаров", "goncharov",
        "некрасов", "nekrasov",
        "островский", "ostrovsky", "ostrovskii",
        "грибоедов", "griboedov", "griboyedov",
        "крылов", "krylov",
        // Английская/европейская классика (авторы умерли задолго до 1955 г.).
        "austen",
        "dickens",
        "twain", "clemens",
        "wilde",
        "doyle",
        "shakespeare",
        "carroll",
        "bronte",
        "shelley",
        "stoker",
        "verne",
        "poe",
        "hawthorne",
        "melville",
        "dumas",
        "hugo",
        "defoe",
        "swift",
        "stevenson",
    ];

    // Фамилии нормализуются тем же способом, что и имя автора (FormD срезает
    // диакритику и бревис: «й»→«и», «ё»→«е»), иначе «Толстой» не совпал бы с «толстои».
    private static readonly string[] Surnames = RawSurnames.Select(Normalize).ToArray();

    public static bool IsAllowed(string? author)
    {
        if (string.IsNullOrWhiteSpace(author))
        {
            return false;
        }

        var normalized = Normalize(author);
        return Surnames.Any(surname => normalized.Contains(surname, StringComparison.Ordinal));
    }

    // Нижний регистр без диакритики и пунктуации: "Tolstoy, Leo, graf, 1828-1910"
    // и "Лев Николаевич Толстой" одинаково матчатся по фамилии.
    private static string Normalize(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetter(ch) ? ch : ' ');
        }

        return builder.ToString();
    }
}
