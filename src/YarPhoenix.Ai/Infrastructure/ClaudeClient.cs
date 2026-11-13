using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace YarPhoenix.Ai.Infrastructure;

// Тонкий typed-клиент Messages API поверх HttpClient: ТЗ §8.1 требует владеть
// HTTP-конвейером (Retry → CircuitBreaker → Timeout через Http.Resilience),
// поэтому SDK с собственными ретраями здесь сознательно не используется.
public sealed class ClaudeClient(HttpClient httpClient)
{
    public async Task<string?> CreateMessageAsync(
        string model,
        int maxTokens,
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var request = new ClaudeMessageRequest(
            model,
            maxTokens,
            // cache_control на стабильном системном промпте (ТЗ §7.5). Примечание:
            // кэшируются префиксы от ~2–4K токенов, короткий промпт просто не закэшируется —
            // маркер остаётся на вырост без вреда.
            [new ClaudeSystemBlock("text", systemPrompt, new ClaudeCacheControl("ephemeral"))],
            [new ClaudeMessage("user", userMessage)]);

        using var response = await httpClient.PostAsJsonAsync("/v1/messages", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ClaudeMessageResponse>(cancellationToken);
        return payload?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
    }
}

internal sealed record ClaudeCacheControl([property: JsonPropertyName("type")] string Type);

internal sealed record ClaudeSystemBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("cache_control")] ClaudeCacheControl CacheControl);

internal sealed record ClaudeMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed record ClaudeMessageRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonPropertyName("system")] IReadOnlyList<ClaudeSystemBlock> System,
    [property: JsonPropertyName("messages")] IReadOnlyList<ClaudeMessage> Messages);

internal sealed class ClaudeMessageResponse
{
    [JsonPropertyName("content")]
    public List<ClaudeContentBlock>? Content { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }
}

internal sealed class ClaudeContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
