namespace YarPhoenix.Catalog.Domain;

// Пороговые награды за стрик (10/30/50/100 дней): выдаются идемпотентно
// и остаются навсегда, даже если стрик потух (ТЗ §4.5).
public sealed class UserAchievement
{
    public static readonly int[] StreakThresholds = [10, 30, 50, 100];

    public Guid UserId { get; set; }

    public int Threshold { get; set; }

    public DateTime UnlockedAtUtc { get; set; }
}
