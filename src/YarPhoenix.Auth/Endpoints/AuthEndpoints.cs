using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.JsonWebTokens;
using YarPhoenix.Auth.Application;
using YarPhoenix.Auth.Contracts;
using YarPhoenix.Shared;

namespace YarPhoenix.Auth.Endpoints;

public static class AuthEndpoints
{
    public const string RateLimiterPolicy = "auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Rate-limit на всю группу — защита от перебора (ТЗ §5.2).
        var group = app.MapGroup("/api/auth").RequireRateLimiting(RateLimiterPolicy);

        group.MapPost("/register", async (
            RegisterRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "Email and password are required." });
            }

            var result = await auth.RegisterAsync(request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        });

        group.MapPost("/login", async (
            LoginRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.LoginAsync(request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        });

        group.MapPost("/refresh", async (
            RefreshRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.RefreshAsync(request.RefreshToken, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        });

        group.MapPost("/logout", async (
            LogoutRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            await auth.LogoutAsync(request.RefreshToken, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/confirm-email", async (
            ConfirmEmailRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.ConfirmEmailAsync(request, cancellationToken);
            return result.IsSuccess ? Results.Ok() : ToErrorResult(result.Error);
        });

        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            await auth.ForgotPasswordAsync(request.Email, cancellationToken);
            // Единый ответ независимо от существования адреса (ТЗ §5.2).
            return Results.Ok(new { message = "If that address is registered, we've sent a link." });
        });

        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.ResetPasswordAsync(request, cancellationToken);
            return result.IsSuccess ? Results.Ok() : ToErrorResult(result.Error);
        });

        group.MapPost("/change-password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal principal,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.ChangePasswordAsync(GetUserId(principal), request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).RequireAuthorization();

        group.MapPost("/change-email", async (
            ChangeEmailRequest request,
            ClaimsPrincipal principal,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.ChangeEmailAsync(GetUserId(principal), request, cancellationToken);
            return result.IsSuccess ? Results.Ok() : ToErrorResult(result.Error);
        }).RequireAuthorization();

        group.MapPost("/confirm-email-change", async (
            ConfirmEmailChangeRequest request,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.ConfirmEmailChangeAsync(request, cancellationToken);
            return result.IsSuccess ? Results.Ok() : ToErrorResult(result.Error);
        });

        group.MapPost("/delete-account", async (
            DeleteAccountRequest request,
            ClaimsPrincipal principal,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.DeleteAccountAsync(GetUserId(principal), request, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : ToErrorResult(result.Error);
        }).RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal principal, AuthService auth) =>
        {
            var result = await auth.GetProfileAsync(GetUserId(principal));
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).RequireAuthorization();

        group.MapPut("/profile", async (
            UpdateProfileRequest request,
            ClaimsPrincipal principal,
            AuthService auth) =>
        {
            var result = await auth.UpdateProfileAsync(GetUserId(principal), request);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).RequireAuthorization();

        group.MapPost("/avatar", async (
            IFormFile file,
            ClaimsPrincipal principal,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            if (file.Length == 0 || !AllowedAvatarTypes.Contains(file.ContentType))
            {
                return Results.BadRequest(new { message = "Upload a JPEG, PNG or WebP image." });
            }

            if (file.Length > MaxAvatarBytes)
            {
                return Results.BadRequest(new { message = "The image must be 2 MB or smaller." });
            }

            await using var content = file.OpenReadStream();
            var result = await auth.UpdateAvatarAsync(
                GetUserId(principal), content, file.Length, file.ContentType, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).RequireAuthorization().DisableAntiforgery();

        group.MapDelete("/avatar", async (
            ClaimsPrincipal principal,
            AuthService auth,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.RemoveAvatarAsync(GetUserId(principal), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToErrorResult(result.Error);
        }).RequireAuthorization();

        return app;
    }

    private const long MaxAvatarBytes = 2 * 1024 * 1024;

    private static readonly string[] AllowedAvatarTypes = ["image/jpeg", "image/png", "image/webp"];

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Authenticated principal has no sub claim."));

    private static IResult ToErrorResult(Error error) =>
        error.Code switch
        {
            "unauthorized" => Results.Json(new { message = error.Message }, statusCode: StatusCodes.Status401Unauthorized),
            "not_found" => Results.NotFound(new { message = error.Message }),
            _ => Results.BadRequest(new { message = error.Message }),
        };
}
