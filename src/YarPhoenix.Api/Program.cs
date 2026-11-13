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
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors(frontendCorsPolicy);
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapCatalogEndpoints();
app.MapLibraryEndpoints();
app.MapAuthEndpoints();
app.MapAssistantEndpoints();

app.Run();
