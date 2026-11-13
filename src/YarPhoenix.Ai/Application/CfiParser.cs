using System.Text.RegularExpressions;

namespace YarPhoenix.Ai.Application;

// Разбор epub.js CFI до границы «прочитанного» (ТЗ §7.2 — анти-спойлер на уровне данных).
// Свойство безопасности: при любой неоднозначности граница смещается НАЗАД (меньше
// контекста), никогда не вперёд — ассистент физически не видит непрочитанного.
public static partial class CfiParser
{
    // Граница = (индекс главы, смещение в символах внутри неё). null → безопасно
    // ограничить нельзя (нет ни CFI, ни выделения) → контекст не строим.
    public static (int ChapterIndex, int Offset)? Resolve(
        string? cfi,
        string? selection,
        IReadOnlyList<string> chapters)
    {
        if (chapters.Count == 0)
        {
            return null;
        }

        var spineIndex = TryParseSpineIndex(cfi, chapters.Count);
        var trimmed = selection?.Trim();

        // Ground truth — само выделение: читатель мог выделить только прочитанное,
        // значит его конец и есть граница. Сначала ищем в главе из CFI, затем назад.
        if (!string.IsNullOrEmpty(trimmed))
        {
            if (spineIndex is int exact)
            {
                var position = chapters[exact].IndexOf(trimmed, StringComparison.Ordinal);
                if (position >= 0)
                {
                    return (exact, position + trimmed.Length);
                }
            }

            var upper = Math.Min(spineIndex ?? chapters.Count - 1, chapters.Count - 1);
            for (var chapter = upper; chapter >= 0; chapter--)
            {
                var position = chapters[chapter].IndexOf(trimmed, StringComparison.Ordinal);
                if (position >= 0)
                {
                    return (chapter, position + trimmed.Length);
                }
            }
        }

        // Выделение не нашли: включаем прошлые главы целиком, текущую — не включаем
        // (offset 0), чтобы точно не зацепить непрочитанное в текущей главе.
        return spineIndex is int index ? (index, 0) : null;
    }

    // Собирает «прочитанное до сих пор»: главы 0..index-1 целиком + начало текущей.
    public static string BuildReadSoFar(
        IReadOnlyList<string> chapters,
        int chapterIndex,
        int offset)
    {
        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < chapterIndex && i < chapters.Count; i++)
        {
            builder.Append(chapters[i]);
            builder.Append('\n');
        }

        if (chapterIndex < chapters.Count)
        {
            var chapter = chapters[chapterIndex];
            builder.Append(chapter[..Math.Clamp(offset, 0, chapter.Length)]);
        }

        return builder.ToString();
    }

    // Индекс главы из части CFI до "!": /6/2N[..] → N-1 (spine-itemref'ы — чётные шаги).
    private static int? TryParseSpineIndex(string? cfi, int chapterCount)
    {
        if (string.IsNullOrWhiteSpace(cfi))
        {
            return null;
        }

        var match = SpineStepRegex().Match(cfi);
        if (!match.Success)
        {
            return null;
        }

        var step = int.Parse(match.Groups[1].Value);
        var index = step / 2 - 1;
        return Math.Clamp(index, 0, chapterCount - 1);
    }

    [GeneratedRegex(@"/6/(\d+)")]
    private static partial Regex SpineStepRegex();
}
