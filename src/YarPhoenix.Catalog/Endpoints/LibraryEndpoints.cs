using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Contracts;
using YarPhoenix.Shared;
using YarPhoenix.Storage;

namespace YarPhoenix.Catalog.Endpoints;

public static class LibraryEndpoints
{
    public static IEndpointRouteBuilder MapLibraryEndpoints(this IEndpointRouteBuilder app)
    {
        // Полки и прогресс привязаны к UserId — только для вошедших (ТЗ §4.4).
        var group = app.MapGroup("/api/library").RequireAuthorization();

        group.MapGet("/books", async (
            ClaimsPrincipal principal,
            LibraryService library,
            CancellationToken cancellationToken,
            string shelf = "all") =>
            Results.Ok(await library.GetShelfAsync(principal.GetUserId(), shelf, cancellationToken)))
            .Produces<IReadOnlyList<LibraryBookResponse>>();

        group.MapPost("/books", async (
            AddToLibraryRequest request,
            ClaimsPrincipal principal,
            LibraryService library,
            CancellationToken cancellationToken) =>
        {
            var result = await library.AddExternalAsync(principal.GetUserId(), request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).Produces<LibraryBookResponse>();

        group.MapPost("/books/read", async (
            StartReadingRequest request,
            ClaimsPrincipal principal,
            LibraryService library,
            CancellationToken cancellationToken) =>
        {
            var result = await library.EnsureReadableAsync(
                principal.GetUserId(), request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).Produces<LibraryBookResponse>();

        group.MapGet("/books/lookup", async (
            string provider,
            string externalId,
            ClaimsPrincipal principal,
            LibraryService library,
            CancellationToken cancellationToken) =>
        {
            var response = await library.LookupAsync(
                principal.GetUserId(), provider, externalId, cancellationToken);
            return response is null ? Results.NotFound() : Results.Ok(response);
        }).Produces<LibraryBookResponse>().Produces(StatusCodes.Status404NotFound);

        group.MapGet("/books/{bookId:guid}", async (
            Guid bookId,
            ClaimsPrincipal principal,
            LibraryService library,
            CancellationToken cancellationToken) =>
        {
            var result = await library.GetOneAsync(principal.GetUserId(), bookId, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).Produces<LibraryBookResponse>();

        group.MapPatch("/books/{bookId:guid}", async (
            Guid bookId,
            UpdateShelfRequest request,
            ClaimsPrincipal principal,
            LibraryService library,
            CancellationToken cancellationToken) =>
        {
            var result = await library.UpdateShelfAsync(
                principal.GetUserId(), bookId, request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).Produces<LibraryBookResponse>();

        group.MapDelete("/books/{bookId:guid}", async (
            Guid bookId,
            ClaimsPrincipal principal,
            LibraryService library,
            CancellationToken cancellationToken) =>
        {
            var result = await library.RemoveAsync(principal.GetUserId(), bookId, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : ToErrorResult(result.Error);
        }).Produces(StatusCodes.Status204NoContent);

        group.MapPut("/books/{bookId:guid}/progress", async (
            Guid bookId,
            ProgressRequest request,
            ClaimsPrincipal principal,
            LibraryService library,
            CancellationToken cancellationToken) =>
        {
            var result = await library.UpdateProgressAsync(
                principal.GetUserId(), bookId, request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).Produces<LibraryBookResponse>();

        group.MapPost("/upload", async (
            IFormFile file,
            ClaimsPrincipal principal,
            LibraryService library,
            IFileStorage fileStorage,
            BookProcessingQueue queue,
            CancellationToken cancellationToken) =>
        {
            // MVP: только EPUB (ТЗ §5.4).
            if (file.Length == 0
                || !file.FileName.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { message = "Only EPUB files are supported." });
            }

            await using var content = file.OpenReadStream();
            var fileId = await fileStorage.UploadBookAsync(
                content, file.Length, "application/epub+zip", cancellationToken);

            var response = await library.CreateUploadedBookAsync(
                principal.GetUserId(), fileId, file.FileName, cancellationToken);

            // Ответ мгновенный; парсинг OPF и матчинг — в фоне (§5.4).
            await queue.EnqueueAsync(response.BookId, cancellationToken);
            return Results.Ok(response);
        }).DisableAntiforgery().Produces<LibraryBookResponse>();

        group.MapGet("/books/{bookId:guid}/file", async (
            Guid bookId,
            ClaimsPrincipal principal,
            LibraryService library,
            IFileStorage fileStorage,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var fileId = await library.GetFileIdAsync(principal.GetUserId(), bookId, cancellationToken);
            if (fileId.IsFailure)
            {
                return ToErrorResult(fileId.Error);
            }

            // Файл книги неизменен (другой файл — другой FileId), поэтому браузер может держать его
            // в своём кэше и перепроверять по ETag. Vary: ответ зависит от того, кто спрашивает.
            httpContext.Response.Headers.CacheControl = "private, max-age=31536000, immutable";
            httpContext.Response.Headers.Vary = HeaderNames.Authorization;

            // Поток из MinIO идёт прямо в ответ: EPUB не собирается в памяти сервера целиком.
            return Results.Stream(
                destination => fileStorage.CopyBookToAsync(fileId.Value, destination, httpContext.RequestAborted),
                "application/epub+zip",
                entityTag: new EntityTagHeaderValue($"\"{fileId.Value}\""));
        }).Produces(StatusCodes.Status200OK, contentType: "application/epub+zip");

        group.MapGet("/stats", async (
            ClaimsPrincipal principal,
            LibraryService library,
            CancellationToken cancellationToken,
            string timeZoneId = "UTC") =>
            Results.Ok(await library.GetStatsAsync(principal.GetUserId(), timeZoneId, cancellationToken)))
            .Produces<LibraryStatsResponse>();

        return app;
    }

    private static IResult ToErrorResult(Error error) =>
        error.Code switch
        {
            "not_found" => Results.NotFound(new { message = error.Message }),
            "unavailable" => Results.Problem(
                title: error.Message, statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.BadRequest(new { message = error.Message }),
        };
}
