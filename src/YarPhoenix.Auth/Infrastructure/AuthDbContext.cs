using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YarPhoenix.Auth.Domain;

namespace YarPhoenix.Auth.Infrastructure;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Своя схема на модуль — граница модульного монолита на уровне БД (ТЗ §3.1).
        builder.HasDefaultSchema("auth");

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Nickname).HasMaxLength(64);
            entity.Property(u => u.UiLanguage).HasMaxLength(8);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.Property(t => t.TokenHash).HasMaxLength(128);
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.UserId);
            entity
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
