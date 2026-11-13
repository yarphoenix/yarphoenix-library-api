using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YarPhoenix.Catalog.Infrastructure;

// Design-time фабрика для dotnet-ef: миграции генерируются без поднятия хоста.
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=127.0.0.1;Port=5433;Database=yarphoenix;Username=yarphoenix;Password=change-me";

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CatalogDbContext(options);
    }
}
