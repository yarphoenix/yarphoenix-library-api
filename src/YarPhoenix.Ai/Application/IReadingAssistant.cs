using YarPhoenix.Ai.Contracts;

namespace YarPhoenix.Ai.Application;

// Единая абстракция ассистента (ТЗ §7.7): внутри — роутинг моделей по шаблону,
// сборка запроса из selection/context/template и кэшируемый префикс.
public interface IReadingAssistant
{
    Task<AssistantResponse> AskAsync(AskAssistantRequest request, CancellationToken cancellationToken);
}
