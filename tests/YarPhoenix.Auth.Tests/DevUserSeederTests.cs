using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YarPhoenix.Auth.Application;
using YarPhoenix.Auth.Domain;
using YarPhoenix.Auth.Infrastructure;

namespace YarPhoenix.Auth.Tests;

// Dev-сидер: создаёт демо-учётку один раз и никогда не переписывает существующую.
public sealed class DevUserSeederTests : IDisposable
{
    private const string Email = "seed@example.com";
    private const string Password = "password123";

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly UserManager<ApplicationUser> _userManager;

    public DevUserSeederTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AuthDbContext>(options => options.UseSqlite(_connection));
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                // Та же парольная политика, что в AuthModule: демо-пароль обязан её проходить.
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AuthDbContext>();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.EnsureCreated();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    [Fact]
    public async Task Seed_CreatesConfirmedUser_WithTheConfiguredPassword()
    {
        await CreateSeeder(Email, Password, "Reader").SeedAsync(CancellationToken.None);

        var user = await _userManager.FindByEmailAsync(Email);
        Assert.NotNull(user);
        Assert.True(user!.EmailConfirmed);
        Assert.Equal("Reader", user.Nickname);
        Assert.True(await _userManager.CheckPasswordAsync(user, Password));
    }

    [Fact]
    public async Task Seed_IsIdempotent_AndKeepsTheExistingUserUntouched()
    {
        await CreateSeeder(Email, Password, "Reader").SeedAsync(CancellationToken.None);

        // Повторный запуск с другими значениями не должен ни дублировать, ни сбрасывать учётку.
        await CreateSeeder(Email, "another-pass-456", "Changed").SeedAsync(CancellationToken.None);

        Assert.Equal(1, await _userManager.Users.CountAsync());
        var user = await _userManager.FindByEmailAsync(Email);
        Assert.Equal("Reader", user!.Nickname);
        Assert.True(await _userManager.CheckPasswordAsync(user, Password));
    }

    [Fact]
    public async Task Seed_DoesNothing_WhenTheSectionIsNotConfigured()
    {
        await CreateSeeder(string.Empty, string.Empty, string.Empty).SeedAsync(CancellationToken.None);

        Assert.Equal(0, await _userManager.Users.CountAsync());
    }

    [Fact]
    public async Task Seed_FallsBackToTheEmailLocalPart_WhenNicknameIsEmpty()
    {
        await CreateSeeder(Email, Password, string.Empty).SeedAsync(CancellationToken.None);

        var user = await _userManager.FindByEmailAsync(Email);
        Assert.Equal("seed", user!.Nickname);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }

    private DevUserSeeder CreateSeeder(string email, string password, string nickname) =>
        new(
            _userManager,
            Options.Create(new DevSeedOptions { Email = email, Password = password, Nickname = nickname }),
            NullLogger<DevUserSeeder>.Instance);
}
