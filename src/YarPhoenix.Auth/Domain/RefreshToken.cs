namespace YarPhoenix.Auth.Domain;

// Хранится только SHA-256 хэш токена: утечка БД не должна отдавать живые refresh-токены.
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    // Хэш токена, выданного взамен при ротации, — след для аудита повторного использования.
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
