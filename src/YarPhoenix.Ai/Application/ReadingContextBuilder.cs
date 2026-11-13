using Microsoft.Extensions.Caching.Hybrid;
using YarPhoenix.Ai.Infrastructure.Epub;
using YarPhoenix.Storage;

namespace YarPhoenix.Ai.Application;

// Собирает контекст ассистента строго до позиции чтения (ТЗ §7.2). Извлечённые главы
// кэшируем по FileId (файл неизменен) — не разбираем EPUB на каждый вопрос.
public sealed class ReadingContextBuilder(IFileStorage fileStorage, HybridCache cache)
{
    private static readonly HybridCacheEntryOptions ChaptersCacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(1),
        LocalCacheExpiration = TimeSpan.FromHours(1),
    };

    public async Task<string?> BuildAsync(
        string fileId,
        string? cfi,
        string? selection,
        string template,
        CancellationToken cancellationToken)
    {
        var chapters = await GetChaptersAsync(fileId, cancellationToken);
        if (chapters.Length == 0)
        {
            return null;
        }

        var boundary = CfiParser.Resolve(cfi, selection, chapters);
        if (boundary is not var (chapterIndex, offset))
        {
            return null;
        }

        var readSoFar = CfiParser.BuildReadSoFar(chapters, chapterIndex, offset);
        if (readSoFar.Length == 0)
        {
            return null;
        }

        var window = AssistantTemplates.ContextWindow(template);
        return ExtractWindow(readSoFar, selection, window);
    }

    private async Task<string[]> GetChaptersAsync(string fileId, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            $"ai:chapters:{fileId}",
            fileId,
            async (id, token) =>
            {
                await using var stream = await fileStorage.OpenBookAsync(id, token);
                return EpubTextExtractor.Extract(stream).ToArray();
            },
            ChaptersCacheOptions,
            cancellationToken: cancellationToken);
    }

    // Окно вокруг выделения (последнее вхождение — ближе к позиции чтения); без
    // выделения или если не нашли — хвост прочитанного (для «кто персонажи»/«что в сцене»).
    private static string ExtractWindow(string readSoFar, string? selection, int window)
    {
        if (readSoFar.Length <= window)
        {
            return readSoFar;
        }

        var trimmed = selection?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            var position = readSoFar.LastIndexOf(trimmed, StringComparison.Ordinal);
            if (position >= 0)
            {
                var half = window / 2;
                var start = Math.Max(0, position - half);
                var end = Math.Min(readSoFar.Length, position + trimmed.Length + half);
                return readSoFar[start..end];
            }
        }

        return readSoFar[^window..];
    }
}
