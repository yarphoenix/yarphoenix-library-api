namespace YarPhoenix.Ai.Contracts;

public sealed record AskAssistantRequest(
    Guid BookId,
    string Template,
    string? Selection,
    string? Context,
    string? Question,
    string Language,
    // Текущая позиция чтения (CFI) — сервер режет по ней контекст (ТЗ §7.2).
    string? Cfi = null);

// Эндпоинт ассистента всегда отвечает 200: при отказе Claude/цепи —
// Available=false + мягкое сообщение, никогда не 500 (ТЗ §8.3).
public sealed record AssistantResponse(bool Available, string? Answer, string? Message)
{
    public static AssistantResponse Ok(string answer) => new(true, answer, null);

    public static AssistantResponse Unavailable(string message) => new(false, null, message);
}
