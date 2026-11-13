using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Polly;
using YarPhoenix.Auth.Application;
using YarPhoenix.Auth.Domain;
using YarPhoenix.Auth.Endpoints;
using YarPhoenix.Auth.Infrastructure;

namespace YarPhoenix.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
        {
            // Fail fast: пустой/короткий секрет в проде — дыра, а не конфиг по умолчанию.
            throw new InvalidOperationException("Jwt:Secret must be configured and at least 32 characters long.");
        }

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName));

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                // Identity-lockout: 5 неудачных попыток → 5 минут (ТЗ §5.2).
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<TokenService>();
        services.AddScoped<AuthService>();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        AddEmailSender(services, configuration);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuerSigningKey = true,
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthEndpoints.RateLimiterPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                    }));
        });

        return services;
    }

    // Пустой Email:ApiKey → dev-режим (ссылки в лог); заполненный → Resend со своим
    // конвейером устойчивости на границе с провайдером (ТЗ §8.1).
    private static void AddEmailSender(IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["Email:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddScoped<IAuthEmailSender, LoggingAuthEmailSender>();
            return;
        }

        services
            .AddHttpClient<IAuthEmailSender, ResendEmailSender>(client =>
            {
                client.BaseAddress = new Uri("https://api.resend.com");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            })
            .AddResilienceHandler("resend", builder =>
            {
                builder
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 2,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        ShouldHandle = args => ValueTask.FromResult(
                            HttpClientResiliencePredicates.IsTransient(args.Outcome)
                            && args.Outcome.Result?.StatusCode != HttpStatusCode.TooManyRequests),
                    })
                    .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = 0.5,
                        MinimumThroughput = 10,
                        SamplingDuration = TimeSpan.FromSeconds(30),
                        BreakDuration = TimeSpan.FromSeconds(15),
                    })
                    .AddTimeout(TimeSpan.FromSeconds(10));
            });
    }
}
