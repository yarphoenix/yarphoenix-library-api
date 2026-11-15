using Microsoft.EntityFrameworkCore;
using YarPhoenix.Catalog.Domain;

namespace YarPhoenix.Catalog.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    public DbSet<UserBook> UserBooks => Set<UserBook>();

    public DbSet<ReaderProfile> ReaderProfiles => Set<ReaderProfile>();

    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

    public DbSet<CollectionImportState> CollectionImportStates => Set<CollectionImportState>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Своя схема на модуль — граница модульного монолита на уровне БД (ТЗ §3.1).
        builder.HasDefaultSchema("catalog");

        // Триграммы — для поиска по подстроке в названии и авторе коллекции (ILIKE '%…%').
        builder.HasPostgresExtension("pg_trgm");

        builder.Entity<Book>(entity =>
        {
            entity.Property(b => b.Title).HasMaxLength(512);
            entity.Property(b => b.Author).HasMaxLength(512);
            entity.Property(b => b.ExternalId).HasMaxLength(64);
            entity.Property(b => b.FileId).HasMaxLength(128);
            entity.Property(b => b.Isbn).HasMaxLength(20);
            entity.Property(b => b.Language).HasMaxLength(8);
            entity.Property(b => b.Provider).HasConversion<string>().HasMaxLength(32);
            entity.Property(b => b.ReadableSource).HasConversion<string>().HasMaxLength(32);
            entity.Property(b => b.SourceRef).HasMaxLength(256);

            // Композитный ключ внешней записи (ТЗ §4.1): id разных провайдеров
            // могут случайно совпасть.
            entity
                .HasIndex(b => new { b.Provider, b.ExternalId })
                .IsUnique()
                .HasFilter("\"Provider\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

            // Витрина коллекции: «самые издаваемые сначала» читается прямо из индекса.
            entity
                .HasIndex(b => b.Popularity)
                .IsDescending()
                .HasFilter("\"InCollection\"");

            entity.HasIndex(b => b.Genres).HasMethod("gin");
            entity.HasIndex(b => b.Title).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasIndex(b => b.Author).HasMethod("gin").HasOperators("gin_trgm_ops");
        });

        builder.Entity<CollectionImportState>(entity =>
        {
            // Строка одна, ключ задаёт код — автоинкремент не нужен.
            entity.Property(s => s.Id).ValueGeneratedNever();
        });

        builder.Entity<UserBook>(entity =>
        {
            entity.HasKey(ub => new { ub.UserId, ub.BookId });
            entity.Property(ub => ub.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(ub => ub.CfiPosition).HasMaxLength(1024);
            entity
                .HasOne(ub => ub.Book)
                .WithMany()
                .HasForeignKey(ub => ub.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReaderProfile>(entity =>
        {
            entity.HasKey(p => p.UserId);
            entity.Property(p => p.TimeZoneId).HasMaxLength(64);
        });

        builder.Entity<UserAchievement>(entity =>
        {
            // Идемпотентность выдачи наград на уровне БД (§4.5).
            entity.HasKey(a => new { a.UserId, a.Threshold });
        });
    }
}
