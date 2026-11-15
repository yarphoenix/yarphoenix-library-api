using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YarPhoenix.Ai.Application;
using YarPhoenix.Ai.Contracts;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Shared;

namespace YarPhoenix.Ai.Endpoints;

public static class AssistantEndpoints
{
    public const string RateLimiterPolicy = "assistant";

    public const int RequestsPerMinute = 20;

    // Потолки длины полей: текст запроса уходит в платный вызов модели, и без них один запрос
    // может стоить как тысяча. Выделение — до пары страниц, вопрос — абзац.
    private const int MaxSelectionLength = 4000;
    private const int MaxQuestionLength = 1000;
    private const int MaxContextLength = 8000;
    private const int MaxCfiLength = 1024;
    private const int MaxShortFieldLength = 32;

    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        // AI — только для вошедших (ТЗ §4.4) и только для книг с файлом:
        // «текст открывает AI», а не полка (§4.4, §9.1 п.8).
        app.MapPost("/api/assistant/ask", async (
            AskAssistantRequest request,
            ClaimsPrincipal principal,
            IReadingAssistant assistant,
            LibraryService library,
            ReadingContextBuilder contextBuilder,
            CancellationToken cancellationToken) =>
        {
            if (FindTooLongField(request) is { } field)
            {
                return Results.BadRequest(new { message = $"Field '{field}' is too long." });
            }

            var userId = principal.GetUserId();
            var book = await library.GetOneAsync(userId, request.BookId, cancellationToken);
            if (book.IsFailure || !book.Value.HasReadableFile)
            {
                return Results.BadRequest(new { message = "The assistant works only on readable books." });
            }

            var effective = request;
            if (AssistantTemplates.RequiresReadContext(request.Template))
            {
                // Анти-спойлер: контекст строит СЕРВЕР по CFI, клиентский Context
                // игнорируется (мог бы содержать непрочитанное). CFI из запроса, иначе
                // сохранённая позиция (§7.2).
                var fileId = await library.GetFileIdAsync(userId, request.BookId, cancellationToken);
                var cfi = string.IsNullOrWhiteSpace(request.Cfi) ? book.Value.CfiPosition : request.Cfi;
                var context = fileId.IsSuccess
                    ? await contextBuilder.BuildAsync(
                        fileId.Value, cfi, request.Selection, request.Template, cancellationToken)
                    : null;
                effective = request with { Context = context };
            }

            // Всегда 200: деградация — в payload (Available=false), не в статусе (§8.3).
            var response = await assistant.AskAsync(effective, cancellationToken);
            return Results.Ok(response);
        })
            .RequireAuthorization()
            .RequireRateLimiting(RateLimiterPolicy)
            .Produces<AssistantResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status429TooManyRequests);

        return app;
    }

    // Имя первого поля длиннее своего потолка (в JSON-написании); null — всё в пределах.
    internal static string? FindTooLongField(AskAssistantRequest request)
    {
        if (request.Selection?.Length > MaxSelectionLength)
        {
            return "selection";
        }

        if (request.Question?.Length > MaxQuestionLength)
        {
            return "question";
        }

        if (request.Context?.Length > MaxContextLength)
        {
            return "context";
        }

        if (request.Cfi?.Length > MaxCfiLength)
        {
            return "cfi";
        }

        if (request.Template.Length > MaxShortFieldLength)
        {
            return "template";
        }

        return request.Language.Length > MaxShortFieldLength ? "language" : null;
    }
}
