namespace YarPhoenix.Ai.Infrastructure;

public sealed class ClaudeOptions
{
    public const string SectionName = "Anthropic";

    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    public string ApiKey { get; set; } = string.Empty;

    // Роутинг моделей по шаблону (ТЗ §7.4): дешёвые задачи → Haiku,
    // интерпретация → Sonnet. Идентификаторы актуальны на июль 2026.
    public string HaikuModel { get; set; } = "claude-haiku-4-5";

    public string SonnetModel { get; set; } = "claude-sonnet-5";
}
