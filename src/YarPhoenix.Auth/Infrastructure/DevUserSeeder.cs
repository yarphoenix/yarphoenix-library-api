using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YarPhoenix.Auth.Application;
using YarPhoenix.Auth.Domain;

namespace YarPhoenix.Auth.Infrastructure;

// Создаёт демо-пользователя для локальной проверки авторизованных страниц.
// Вызывать ТОЛЬКО в Development: учётка с известным паролем в проде — дыра.
// Идемпотентен: существующего пользователя не трогает (пароль и профиль не сбрасывает).
public sealed class DevUserSeeder(
    UserManager<ApplicationUser> userManager,
    IOptions<DevSeedOptions> options,
    ILogger<DevUserSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
        {
            return;
        }

        var email = seed.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        // UserManager не принимает токен — проверяем отмену сами перед записью.
        cancellationToken.ThrowIfCancellationRequested();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Nickname = string.IsNullOrWhiteSpace(seed.Nickname) ? email.Split('@')[0] : seed.Nickname.Trim(),
            // Письмо демо-учётке не отправляем — почта считается подтверждённой сразу.
            EmailConfirmed = true,
        };

        var created = await userManager.CreateAsync(user, seed.Password);
        if (created.Succeeded)
        {
            logger.LogInformation("Seeded development user {Email}", email);
        }
        else
        {
            logger.LogWarning(
                "Development user {Email} was not seeded: {Errors}",
                email,
                string.Join("; ", created.Errors.Select(error => error.Description)));
        }
    }
}
