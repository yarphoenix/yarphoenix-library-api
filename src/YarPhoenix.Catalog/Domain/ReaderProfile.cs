namespace YarPhoenix.Catalog.Domain;

// Стрик-состояние читателя: O(1)-обновление без пересчёта истории (ТЗ §4.5).
// Живёт в схеме catalog отдельно от Identity-пользователя — модуль владеет своим.
public sealed class ReaderProfile
{
    public Guid UserId { get; set; }

    public int CurrentStreak { get; set; }

    public int LongestStreak { get; set; }

    // «День» считается по таймзоне пользователя, не по UTC (§4.5).
    public DateOnly? LastActiveDate { get; set; }

    public string TimeZoneId { get; set; } = "UTC";
}
