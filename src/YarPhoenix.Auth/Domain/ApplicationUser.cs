using Microsoft.AspNetCore.Identity;

namespace YarPhoenix.Auth.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string Nickname { get; set; } = string.Empty;

    // Любимые жанры профиля (ТЗ §5.2.1); ключи совпадают с фильтрами каталога.
    public List<string> FavoriteGenres { get; set; } = [];

    // 'ru' | 'en' — язык интерфейса (§5.2.1).
    public string UiLanguage { get; set; } = "ru";

    // Ключ объекта в публичном бакете avatars (§5.3); null — аватар не загружен.
    public string? AvatarKey { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
