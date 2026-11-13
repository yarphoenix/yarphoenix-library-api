using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Contracts;
using YarPhoenix.Catalog.Infrastructure.OpenLibrary;

namespace YarPhoenix.Catalog.Endpoints;

public static class CatalogEndpoints
{
    private const int MaxPageSize = 50;

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog");

        group.MapGet("/books", async (
            CatalogService catalog,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 24,
            string? q = null,
            string? genre = null,
            string? lang = null) =>
        {
            var query = new BookSearchQuery(
                Math.Max(page, 1),
                Math.Clamp(pageSize, 1, MaxPageSize),
                q,
                genre,
                lang);

            var result = await catalog.SearchAsync(query, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(title: result.Error.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }).Produces<BookListResponse>().ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/books/{provider}/{externalId}", async (
            string provider,
            string externalId,
            CatalogService catalog,
            CancellationToken cancellationToken) =>
        {
            // Внешняя запись уникальна парой (Provider, ExternalId) — ТЗ §4.1;
            // сейчас метаданные отдаёт единственный провайдер.
            if (!string.Equals(provider, OpenLibraryProvider.ProviderName, StringComparison.OrdinalIgnoreCase))
            {
                return Results.NotFound();
            }

            var result = await catalog.GetDetailsAsync(externalId, cancellationToken);
            if (result.IsSuccess)
            {
                return Results.Ok(result.Value);
            }

            return result.Error.Code == "not_found"
                ? Results.NotFound()
                : Results.Problem(title: result.Error.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
            .Produces<BookDetailsResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }
}
