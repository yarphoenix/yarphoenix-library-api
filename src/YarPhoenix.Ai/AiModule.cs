using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using YarPhoenix.Ai.Application;
using YarPhoenix.Ai.Endpoints;
using YarPhoenix.Ai.Infrastructure;

namespace YarPhoenix.Ai;

public static class AiModule
{
    public static IServiceCollection AddAiModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.SectionName));

        // Безопасно вызывать повторно (TryAdd внутри): кэш глав для CFI-обрезки (§7.2).
        services.AddHybridCache();
        services.AddScoped<ReadingContextBuilder>();

        services
            .AddHttpClient<ClaudeClient>((serviceProvider, client) =>
            {
                var options = configuration.GetSection(ClaudeOptions.SectionName).Get<ClaudeOptions>()
                    ?? new ClaudeOptions();
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            })
            .AddResilienceHandler("claude", builder =>
            {
                // Конвейер устойчивости вокруг Claude (ТЗ §8.1),
                // снаружи внутрь: Retry → CircuitBreaker → Timeout (на попытку).
                builder
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        // Только транзиентные сбои; 429 и 4xx не ретраим (§8.1).
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
                    .AddTimeout(TimeSpan.FromSeconds(20));
            });

        services.AddScoped<IReadingAssistant, ClaudeReadingAssistant>();

        // Каждый вопрос ассистенту — платный вызов модели, поэтому лимит считается на пользователя
        // (эндпоинт только для вошедших); адрес — запасной ключ на случай запроса без токена.
        services.AddRateLimiter(options =>
            options.AddPolicy(AssistantEndpoints.RateLimiterPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = AssistantEndpoints.RequestsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                    })));

        return services;
    }
}
