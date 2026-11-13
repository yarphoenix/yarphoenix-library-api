using System.Text;

namespace YarPhoenix.Ai.Application;

internal sealed record TemplatePlan(string Model, int MaxTokens, string UserMessage);

// Шаблоны MVP (ТЗ §9.1 п.8): дешёвые (значение слова, перевод, пересказ) → Haiku,
// свободный вопрос → Sonnet (§7.4). Интерпретационные шаблоны добавляются позже (§9.2).
internal static class AssistantTemplates
{
    public const string Meaning = "meaning";
    public const string Translate = "translate";
    public const string Summarize = "summarize";
    public const string Ask = "ask";

    // Интерпретационные шаблоны (ТЗ §7.3/§7.4, вторая итерация §21) → Sonnet.
    public const string Explain = "explain";
    public const string AuthorMeans = "authorMeans";
    public const string Reference = "reference";
    public const string Scene = "scene";
    public const string Characters = "characters";

    public static readonly string[] Known =
        [Meaning, Translate, Summarize, Ask, Explain, AuthorMeans, Reference, Scene, Characters];

    // Шаблонам, которым нужен контекст «прочитанного до сих пор» с обрезкой по CFI (§7.2).
    // Дешёвые (значение/перевод/пересказ) работают по одному выделению — контекст не нужен.
    public static bool RequiresReadContext(string template) =>
        template is Explain or AuthorMeans or Reference or Scene or Characters;

    // Размер окна контекста по шаблону (символы, ТЗ §7.3).
    public static int ContextWindow(string template) =>
        template switch
        {
            Explain or AuthorMeans => 1000,
            Reference => 300,
            Scene => 1500,
            Characters => 2000,
            _ => 1000,
        };

    // Анти-спойлер системный промпт (ТЗ §7.2) — стабильный, помечается для prompt caching.
    public const string SystemPrompt =
        "You are helping a reader as they progress through a book. You are given only the text " +
        "the reader has already read. Answer strictly on its basis. Never speculate or mention " +
        "events not present in the provided fragment. If the answer isn't in the text, say that " +
        "there was no information up to this point in the book yet. Keep answers short: a few " +
        "sentences at most. Answer in the language the reader asks in.";

    public static bool TryBuildPlan(
        string template,
        string? selection,
        string? context,
        string? question,
        string language,
        string haikuModel,
        string sonnetModel,
        out TemplatePlan plan)
    {
        plan = null!;
        var answerLanguage = language == "en" ? "English" : "Russian";
        var body = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(context))
        {
            body.AppendLine("Text the reader has already read (context):");
            body.AppendLine($"\"{context.Trim()}\"");
            body.AppendLine();
        }

        switch (template)
        {
            case Meaning when !string.IsNullOrWhiteSpace(selection):
                body.AppendLine($"The reader selected: \"{selection.Trim()}\"");
                body.Append($"Explain what it means in this context. Answer in {answerLanguage}.");
                plan = new TemplatePlan(haikuModel, 400, body.ToString());
                return true;

            case Translate when !string.IsNullOrWhiteSpace(selection):
                body.AppendLine($"The reader selected: \"{selection.Trim()}\"");
                body.Append(language == "en"
                    ? "Translate it into English and briefly note any nuance."
                    : "Translate it into Russian and briefly note any nuance.");
                plan = new TemplatePlan(haikuModel, 400, body.ToString());
                return true;

            case Summarize when !string.IsNullOrWhiteSpace(selection):
                body.AppendLine($"The reader selected this passage: \"{selection.Trim()}\"");
                body.Append($"Summarize it briefly. Answer in {answerLanguage}.");
                plan = new TemplatePlan(haikuModel, 400, body.ToString());
                return true;

            case Ask when !string.IsNullOrWhiteSpace(question):
                if (!string.IsNullOrWhiteSpace(selection))
                {
                    body.AppendLine($"The reader selected: \"{selection.Trim()}\"");
                }

                body.AppendLine($"The reader asks: {question.Trim()}");
                body.Append($"Answer in {answerLanguage}.");
                plan = new TemplatePlan(sonnetModel, 600, body.ToString());
                return true;

            case Explain when !string.IsNullOrWhiteSpace(selection):
                body.AppendLine($"The reader selected: \"{selection.Trim()}\"");
                body.Append(
                    "Explain it in simple words, using only the context above. "
                    + $"Answer in {answerLanguage}.");
                plan = new TemplatePlan(sonnetModel, 600, body.ToString());
                return true;

            case AuthorMeans when !string.IsNullOrWhiteSpace(selection):
                body.AppendLine($"The reader selected: \"{selection.Trim()}\"");
                body.Append(
                    "What does the author mean by this? Base your answer only on the context above. "
                    + $"Answer in {answerLanguage}.");
                plan = new TemplatePlan(sonnetModel, 600, body.ToString());
                return true;

            case Reference when !string.IsNullOrWhiteSpace(selection):
                body.AppendLine($"The reader selected: \"{selection.Trim()}\"");
                // Аллюзия опирается на внешнее знание, но связывать её с более поздним
                // сюжетом запрещено — иначе ранняя отсылка выдаст финал (ТЗ §7.3).
                body.Append(
                    "Is there a reference or allusion here? You may use general knowledge, but "
                    + "never connect it to events that happen later in this book. "
                    + $"Answer in {answerLanguage}.");
                plan = new TemplatePlan(sonnetModel, 500, body.ToString());
                return true;

            case Scene:
                if (!string.IsNullOrWhiteSpace(selection))
                {
                    body.AppendLine($"The reader is looking at: \"{selection.Trim()}\"");
                }

                body.Append(
                    "What is happening in this scene? Base your answer only on the context above. "
                    + $"Answer in {answerLanguage}.");
                plan = new TemplatePlan(sonnetModel, 700, body.ToString());
                return true;

            case Characters:
                body.Append(
                    "Who are the characters involved so far? Base your answer only on the context "
                    + $"above (what the reader has read). Answer in {answerLanguage}.");
                plan = new TemplatePlan(sonnetModel, 800, body.ToString());
                return true;

            default:
                return false;
        }
    }
}
