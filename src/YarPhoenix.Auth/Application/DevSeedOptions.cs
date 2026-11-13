namespace YarPhoenix.Auth.Application;

// Демо-учётка для локальной разработки (README, «Локальный запуск»). Секция есть только
// в appsettings.Development.json; пустой Email или Password — сидер ничего не создаёт.
public sealed class DevSeedOptions
{
    public const string SectionName = "DevSeed";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Nickname { get; set; } = string.Empty;
}
