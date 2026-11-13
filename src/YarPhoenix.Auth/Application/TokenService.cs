using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using YarPhoenix.Auth.Domain;

namespace YarPhoenix.Auth.Application;

public sealed class TokenService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public (string AccessToken, int ExpiresInSeconds) CreateAccessToken(ApplicationUser user)
    {
        var lifetime = TimeSpan.FromMinutes(_options.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = DateTime.UtcNow.Add(lifetime),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("nickname", user.Nickname),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, (int)lifetime.TotalSeconds);
    }

    // Криптослучайный opaque-токен; в БД попадает только его хэш.
    public static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public RefreshToken CreateRefreshTokenEntity(Guid userId, string refreshToken) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(refreshToken),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenDays),
        };
}
