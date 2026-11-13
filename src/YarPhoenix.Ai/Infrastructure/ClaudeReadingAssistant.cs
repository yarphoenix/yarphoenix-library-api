using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using YarPhoenix.Ai.Application;
using YarPhoenix.Ai.Contracts;

namespace YarPhoenix.Ai.Infrastructure;

public sealed class ClaudeReadingAssistant(
    ClaudeClient client,
    IOptions<ClaudeOptions> options,
    ILogger<ClaudeReadingAssistant> logger) : IReadingAssistant
{
    private const string UnavailableMessage =
        "The assistant is temporarily unavailable. The book is still all yours — try again in a minute.";

    private readonly ClaudeOptions _options = options.Value;

    public async Task<AssistantResponse> AskAsync(
        AskAssistantRequest request,
        CancellationToken cancellationToken)
    {
        if (!AssistantTemplates.TryBuildPlan(
                request.Template,
                request.Selection,
                request.Context,
                request.Question,
                request.Language,
                _options.HaikuModel,
                _options.SonnetModel,
                out var plan))
        {
            return AssistantResponse.Unavailable("Unknown assistant template.");
        }

        // Нет ключа — честная мягкая деградация вместо падения (§8.3).
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return AssistantResponse.Unavailable(UnavailableMessage);
        }

        try
        {
            var answer = await client.CreateMessageAsync(
                plan.Model,
                plan.MaxTokens,
                AssistantTemplates.SystemPrompt,
                plan.UserMessage,
                cancellationToken);

            return string.IsNullOrWhiteSpace(answer)
                ? AssistantResponse.Unavailable(UnavailableMessage)
                : AssistantResponse.Ok(answer);
        }
        catch (Exception exception) when (IsDependencyFailure(exception, cancellationToken))
        {
            // Fallback возвращает ЗНАЧЕНИЕ, а не исключение (ТЗ §8.2): падение
            // ассистента не должно уронить запрос читалки.
            logger.LogWarning(exception, "Assistant call failed; returning degraded response");
            return AssistantResponse.Unavailable(UnavailableMessage);
        }
    }

    private static bool IsDependencyFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or BrokenCircuitException or TimeoutRejectedException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);
}
