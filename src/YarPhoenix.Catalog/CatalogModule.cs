using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Infrastructure;
using YarPhoenix.Catalog.Infrastructure.Epub;
using YarPhoenix.Catalog.Infrastructure.InternetArchive;
using YarPhoenix.Catalog.Infrastructure.OpenLibrary;
using YarPhoenix.Shared;

namespace YarPhoenix.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHybridCache();
        services.AddScoped<CatalogService>();
        services.AddScoped<LibraryService>();
        services.AddScoped<IUserDataRemover, UserDataRemover>();
        services.AddSingleton<BookProcessingQueue>();
        services.AddHostedService<EpubProcessingWorker>();

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services
            .AddHttpClient<IBookMetadataProvider, OpenLibraryProvider>(client =>
            {
                var baseUrl = configuration["OpenLibrary:BaseUrl"] ?? "https://openlibrary.org";
                client.BaseAddress = new Uri(baseUrl);
                // Open Library просит идентифицирующий User-Agent.
                client.DefaultRequestHeaders.UserAgent.ParseAdd("YarPhoenixLibrary/0.1 (portfolio project)");
            })
            .AddResilienceHandler("open-library", builder =>
            {
                // Полный конвейер устойчивости на границе с внешним источником (ТЗ §8.1),
                // снаружи внутрь: Retry → CircuitBreaker → Timeout (на попытку).
                builder
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 2,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        // Транзиентные сбои без 429: rate-limit ретраить нельзя.
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

        // Слот 2 — Internet Archive (ТЗ §5.1.2): свой клиент и своя копия политик
        // устойчивости на этой границе (§8.1). Таймаут больше — скачивание EPUB.
        services
            .AddHttpClient<IPublicDomainSource, InternetArchiveSource>(client =>
            {
                var baseUrl = configuration["InternetArchive:BaseUrl"] ?? "https://archive.org";
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("YarPhoenixLibrary/0.1 (portfolio project)");
            })
            .AddResilienceHandler("internet-archive", builder =>
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
                    // Скачивание PD-файла (единицы МБ) через прокси — до минуты на попытку.
                    .AddTimeout(TimeSpan.FromSeconds(60));
            });

        return services;
    }
}
