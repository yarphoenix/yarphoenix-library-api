using System.Text.Json;
using YarPhoenix.Shared;

namespace YarPhoenix.Api;

// Тело запроса без обязательного поля — это 400 с именем поля, а не NullReferenceException и 500.
// Фильтр висит на корневой группе маршрутов и потому действует на эндпоинты всех модулей.
internal sealed class RequiredFieldsFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is not null
                && IsRequestBody(argument.GetType())
                && RequiredFields.FindMissing(argument) is { } property)
            {
                var field = JsonNamingPolicy.CamelCase.ConvertName(property);
                return Results.BadRequest(new { message = $"Field '{field}' is required." });
            }
        }

        return await next(context);
    }

    // Тела запросов — записи *Request из контрактов модулей.
    private static bool IsRequestBody(Type type) =>
        type.Name.EndsWith("Request", StringComparison.Ordinal)
        && type.Namespace?.StartsWith("YarPhoenix.", StringComparison.Ordinal) == true;
}
