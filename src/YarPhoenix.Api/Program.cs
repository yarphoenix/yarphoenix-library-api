using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using YarPhoenix.Ai;
using YarPhoenix.Ai.Endpoints;
using YarPhoenix.Auth;
using YarPhoenix.Auth.Endpoints;
using YarPhoenix.Auth.Infrastructure;
using YarPhoenix.Catalog;
using YarPhoenix.Catalog.Endpoints;
using YarPhoenix.Catalog.Infrastructure;
using YarPhoenix.Storage;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    // Фронтенд живёт на другом origin (gh-pages / dev-сервер) — ТЗ §11.4.
    options.AddPolicy(frontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddProblemDetails();

// Числа в JSON — только числами. Web-дефолт допускает и строки ("42"), из-за чего OpenAPI
// описывает каждое числовое поле как «число или строка» и типы фронтенда расплываются.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

// Некорректное тело запроса — это 400 в любом окружении. По умолчанию в Development минимальные
// API бросают исключение (и оно уходит в 500), а в Production молча отвечают 400.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

// OpenAPI-документ — источник типов для фронтенда (`npm run gen:api`); отдаётся только в Development.
builder.Services.AddOpenApi();

builder.Services.AddCatalogModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddStorageModule(builder.Configuration);
builder.Services.AddAiModule(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AuthDbContext>("postgres-auth")
    .AddDbContextCheck<CatalogDbContext>("postgres-catalog");

var app = builder.Build();

// Миграции на старте — осознанный выбор для монолита (ТЗ §11.2); при
// масштабировании на несколько инстансов вынести в отдельный шаг деплоя.
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();

    // Демо-учётка с известным паролем — только для локальной разработки, в проде её быть не должно.
    if (app.Environment.IsDevelopment())
    {
        await scope.ServiceProvider.GetRequiredService<DevUserSeeder>().SeedAsync(app.Lifetime.ApplicationStopping);
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors(frontendCorsPolicy);
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapCatalogEndpoints();
app.MapLibraryEndpoints();
app.MapAuthEndpoints();
app.MapAssistantEndpoints();

app.Run();
