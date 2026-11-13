using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using YarPhoenix.Auth.Application;
using YarPhoenix.Auth.Domain;
using YarPhoenix.Auth.Infrastructure;

namespace YarPhoenix.Auth.Tests;

// Ротация и reuse-detection refresh-токенов (ТЗ §5.2) — путь RefreshAsync не трогает
// UserManager, поэтому его (и другие несмежные зависимости) передаём заглушками.
public sealed class AuthServiceRefreshTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AuthDbContext _dbContext;
    private readonly TokenService _tokenService;
    private readonly AuthService _service;

    public AuthServiceRefreshTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new AuthDbContext(options);
        _dbContext.Database.EnsureCreated();

        var jwt = Options.Create(new JwtOptions
        {
            Secret = new string('k', 40),
            Issuer = "test",
            Audience = "test",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 14,
        });
        _tokenService = new TokenService(jwt);

        _service = new AuthService(
            userManager: null!,
            signInManager: null!,
            _dbContext,
            _tokenService,
            emailSender: null!,
            fileStorage: null!,
            userDataRemover: null!,
            frontendOptions: null!,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task Refresh_RotatesToken_RevokingTheOldOne()
    {
        var (_, raw) = await SeedUserWithTokenAsync();

        var result = await _service.RefreshAsync(raw, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(raw, result.Value.RefreshToken);

        var old = await FindByRawAsync(raw);
        Assert.NotNull(old!.RevokedAtUtc);
        Assert.NotNull(old.ReplacedByTokenHash);

        var fresh = await FindByRawAsync(result.Value.RefreshToken);
        Assert.NotNull(fresh);
        Assert.True(fresh!.IsActive);
    }

    [Fact]
    public async Task Refresh_ReusingRotatedToken_RevokesAllUserTokens()
    {
        var (userId, raw) = await SeedUserWithTokenAsync();

        var rotated = await _service.RefreshAsync(raw, CancellationToken.None);
        Assert.True(rotated.IsSuccess);

        // Повторное предъявление уже ротированного токена — признак кражи.
        var reuse = await _service.RefreshAsync(raw, CancellationToken.None);
        Assert.True(reuse.IsFailure);

        var active = await _dbContext.RefreshTokens
            .CountAsync(t => t.UserId == userId && t.RevokedAtUtc == null);
        Assert.Equal(0, active);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Fails()
    {
        var user = await SeedUserAsync();
        var raw = TokenService.GenerateRefreshToken();
        var entity = _tokenService.CreateRefreshTokenEntity(user.Id, raw);
        entity.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        _dbContext.RefreshTokens.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _service.RefreshAsync(raw, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Refresh_UnknownToken_Fails()
    {
        var result = await _service.RefreshAsync(
            TokenService.GenerateRefreshToken(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private async Task<ApplicationUser> SeedUserAsync()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"user-{Guid.NewGuid():N}@example.com",
            Email = "reader@example.com",
            Nickname = "Reader",
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    private async Task<(Guid UserId, string Raw)> SeedUserWithTokenAsync()
    {
        var user = await SeedUserAsync();
        var raw = TokenService.GenerateRefreshToken();
        _dbContext.RefreshTokens.Add(_tokenService.CreateRefreshTokenEntity(user.Id, raw));
        await _dbContext.SaveChangesAsync();
        return (user.Id, raw);
    }

    private Task<RefreshToken?> FindByRawAsync(string raw)
    {
        var hash = TokenService.HashToken(raw);
        return _dbContext.RefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(t => t.TokenHash == hash);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
