using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YarPhoenix.Auth.Contracts;
using YarPhoenix.Auth.Domain;
using YarPhoenix.Auth.Infrastructure;
using YarPhoenix.Shared;
using YarPhoenix.Storage;

namespace YarPhoenix.Auth.Application;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AuthDbContext dbContext,
    TokenService tokenService,
    IAuthEmailSender emailSender,
    IFileStorage fileStorage,
    IUserDataRemover userDataRemover,
    IOptions<FrontendOptions> frontendOptions,
    ILogger<AuthService> logger)
{
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("Invalid email or password.");

    private static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Refresh token is invalid or expired.");

    public async Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Nickname = string.IsNullOrWhiteSpace(request.Nickname)
                ? email.Split('@')[0]
                : request.Nickname.Trim(),
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Result.Failure<RegisterResponse>(Error.Validation(DescribeErrors(created)));
        }

        var emailSent = await TrySendConfirmationEmailAsync(user, cancellationToken);
        return Result.Success(new RegisterResponse(emailSent));
    }

    public async Task<Result<TokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Result.Failure<TokenResponse>(InvalidCredentials);
        }

        // lockoutOnFailure: защита от перебора поверх rate-limit (ТЗ §5.2).
        var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            return Result.Failure<TokenResponse>(InvalidCredentials);
        }

        return Result.Success(await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<Result<TokenResponse>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var hash = TokenService.HashToken(refreshToken);
        var stored = await dbContext.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored?.User is null)
        {
            return Result.Failure<TokenResponse>(InvalidRefreshToken);
        }

        if (!stored.IsActive)
        {
            // Повторное использование уже ротированного токена — признак кражи:
            // гасим все активные сессии пользователя.
            if (stored.RevokedAtUtc is not null)
            {
                logger.LogWarning("Refresh token reuse detected for user {UserId}", stored.UserId);
                await RevokeAllTokensAsync(stored.UserId, cancellationToken);
            }

            return Result.Failure<TokenResponse>(InvalidRefreshToken);
        }

        var response = await IssueTokensAsync(stored.User, cancellationToken, rotatedFrom: stored);
        return Result.Success(response);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = TokenService.HashToken(refreshToken);
        var stored = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is { RevokedAtUtc: null })
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.Validation("Confirmation link is invalid."));
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(Error.Validation("Confirmation link is invalid or expired."));
    }

    // Ответ всегда одинаковый: нельзя раскрывать, существует ли адрес (ТЗ §5.2).
    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = BuildFrontendLink("reset-password", user.Id, token);
        var sent = await emailSender.SendPasswordResetEmailAsync(user.Email!, link, cancellationToken);
        if (sent.IsFailure)
        {
            logger.LogWarning("Password reset email was not sent to {Email}: {Reason}", email, sent.Error.Message);
        }
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.Validation("Reset link is invalid."));
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return Result.Failure(Error.Validation(DescribeErrors(result)));
        }

        // Смена пароля инвалидирует все выданные refresh-токены.
        await RevokeAllTokensAsync(user.Id, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<TokenResponse>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<TokenResponse>(InvalidCredentials);
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return Result.Failure<TokenResponse>(Error.Validation(DescribeErrors(result)));
        }

        await RevokeAllTokensAsync(user.Id, cancellationToken);
        return Result.Success(await IssueTokensAsync(user, cancellationToken));
    }

    // Смена почты (ТЗ §5.2.1): требует текущий пароль, шлёт токен-ссылку на НОВЫЙ
    // адрес; та же токен-механика Identity, что и подтверждение почты.
    public async Task<Result> ChangeEmailAsync(
        Guid userId,
        ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("User was not found."));
        }

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            return Result.Failure(Error.Unauthorized("Current password is incorrect."));
        }

        var newEmail = request.NewEmail.Trim();
        if (string.IsNullOrWhiteSpace(newEmail))
        {
            return Result.Failure(Error.Validation("New email must not be empty."));
        }

        if (await userManager.FindByEmailAsync(newEmail) is not null)
        {
            return Result.Failure(Error.Validation("That email is already in use."));
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var link = BuildFrontendLink(
            "confirm-email-change", user.Id, token, $"&email={WebUtility.UrlEncode(newEmail)}");
        var sent = await emailSender.SendConfirmationEmailAsync(newEmail, link, cancellationToken);
        return sent.IsSuccess
            ? Result.Success()
            : Result.Failure(Error.Unavailable("Couldn't send the confirmation email. Try again later."));
    }

    public async Task<Result> ConfirmEmailChangeAsync(
        ConfirmEmailChangeRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.Validation("Confirmation link is invalid."));
        }

        var result = await userManager.ChangeEmailAsync(user, request.NewEmail, request.Token);
        if (!result.Succeeded)
        {
            return Result.Failure(Error.Validation("Confirmation link is invalid or expired."));
        }

        // Логин по email — держим UserName в паре с адресом; смена гасит сессии.
        await userManager.SetUserNameAsync(user, request.NewEmail);
        await RevokeAllTokensAsync(user.Id, cancellationToken);
        return Result.Success();
    }

    // Удаление аккаунта (ТЗ §5.2.1): пароль обязателен, затем чистим доменные данные
    // (полки/стрик/достижения, файлы-сирот), аватар и самого пользователя (токены — каскадом).
    public async Task<Result> DeleteAccountAsync(
        Guid userId,
        DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("User was not found."));
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result.Failure(Error.Unauthorized("Password is incorrect."));
        }

        await userDataRemover.RemoveUserDataAsync(userId, cancellationToken);

        if (user.AvatarKey is { } key)
        {
            await fileStorage.DeleteAvatarAsync(key, cancellationToken);
        }

        await userManager.DeleteAsync(user);
        return Result.Success();
    }

    public async Task<Result<UserResponse>> GetProfileAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null
            ? Result.Failure<UserResponse>(Error.NotFound("User was not found."))
            : Result.Success(ToUserResponse(user));
    }

    public async Task<Result<UserResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<UserResponse>(Error.NotFound("User was not found."));
        }

        if (string.IsNullOrWhiteSpace(request.Nickname))
        {
            return Result.Failure<UserResponse>(Error.Validation("Nickname must not be empty."));
        }

        user.Nickname = request.Nickname.Trim();
        user.FavoriteGenres = request.FavoriteGenres.Take(20).ToList();
        user.UiLanguage = request.UiLanguage is "en" or "ru" ? request.UiLanguage : user.UiLanguage;

        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? Result.Success(ToUserResponse(user))
            : Result.Failure<UserResponse>(Error.Validation(DescribeErrors(result)));
    }

    public async Task<Result<UserResponse>> UpdateAvatarAsync(
        Guid userId,
        Stream content,
        long size,
        string contentType,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<UserResponse>(Error.NotFound("User was not found."));
        }

        var previousKey = user.AvatarKey;
        user.AvatarKey = await fileStorage.UploadAvatarAsync(
            userId, content, size, contentType, cancellationToken);
        await userManager.UpdateAsync(user);

        // Старый файл удаляем после успешной подмены — обход кэша и без сирот.
        if (previousKey is not null)
        {
            await fileStorage.DeleteAvatarAsync(previousKey, cancellationToken);
        }

        return Result.Success(ToUserResponse(user));
    }

    public async Task<Result<UserResponse>> RemoveAvatarAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<UserResponse>(Error.NotFound("User was not found."));
        }

        if (user.AvatarKey is { } key)
        {
            user.AvatarKey = null;
            await userManager.UpdateAsync(user);
            await fileStorage.DeleteAvatarAsync(key, cancellationToken);
        }

        return Result.Success(ToUserResponse(user));
    }

    private async Task<TokenResponse> IssueTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken,
        RefreshToken? rotatedFrom = null)
    {
        var refreshToken = TokenService.GenerateRefreshToken();
        var entity = tokenService.CreateRefreshTokenEntity(user.Id, refreshToken);

        if (rotatedFrom is not null)
        {
            rotatedFrom.RevokedAtUtc = DateTime.UtcNow;
            rotatedFrom.ReplacedByTokenHash = entity.TokenHash;
        }

        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var (accessToken, expiresIn) = tokenService.CreateAccessToken(user);
        return new TokenResponse(accessToken, expiresIn, refreshToken, ToUserResponse(user));
    }

    private async Task RevokeAllTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow),
                cancellationToken);
    }

    private async Task<bool> TrySendConfirmationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = BuildFrontendLink("confirm-email", user.Id, token);
        var sent = await emailSender.SendConfirmationEmailAsync(user.Email!, link, cancellationToken);
        if (sent.IsFailure)
        {
            logger.LogWarning("Confirmation email was not sent to {Email}: {Reason}", user.Email, sent.Error.Message);
        }

        return sent.IsSuccess;
    }

    private string BuildFrontendLink(string route, Guid userId, string token, string extraQuery = "")
    {
        var baseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/{route}?userId={userId}&token={WebUtility.UrlEncode(token)}{extraQuery}";
    }

    private static string DescribeErrors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));

    private UserResponse ToUserResponse(ApplicationUser user) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.Nickname,
            user.FavoriteGenres,
            user.UiLanguage,
            user.EmailConfirmed,
            user.AvatarKey is null ? null : fileStorage.BuildAvatarUrl(user.AvatarKey));
}
