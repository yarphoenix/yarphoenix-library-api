using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YarPhoenix.Auth.Infrastructure;

// Design-time фабрика для dotnet-ef: миграции генерируются без поднятия хоста.
public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=127.0.0.1;Port=5433;Database=yarphoenix;Username=yarphoenix;Password=change-me";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AuthDbContext(options);
    }
}
