namespace YarPhoenix.Auth.Contracts;

public sealed record RegisterRequest(string Email, string Password, string? Nickname);

public sealed record RegisterResponse(bool EmailSent);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ConfirmEmailRequest(Guid UserId, string Token);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ChangeEmailRequest(string NewEmail, string CurrentPassword);

public sealed record ConfirmEmailChangeRequest(Guid UserId, string Token, string NewEmail);

public sealed record DeleteAccountRequest(string Password);

public sealed record UpdateProfileRequest(string Nickname, List<string> FavoriteGenres, string UiLanguage);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string Nickname,
    List<string> FavoriteGenres,
    string UiLanguage,
    bool EmailConfirmed,
    string? AvatarUrl);

public sealed record TokenResponse(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    UserResponse User);
