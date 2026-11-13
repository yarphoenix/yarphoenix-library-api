using YarPhoenix.Catalog.Domain;

namespace YarPhoenix.Catalog.Application;

// Чистая логика стрика (ТЗ §4.5): O(1), без сканирования истории.
internal static class StreakLogic
{
    public static void Advance(ReaderProfile profile, DateOnly today)
    {
        if (profile.LastActiveDate == today)
        {
            return;
        }

        profile.CurrentStreak = profile.LastActiveDate == today.AddDays(-1)
            ? profile.CurrentStreak + 1
            : 1;

        profile.LastActiveDate = today;
        profile.LongestStreak = Math.Max(profile.LongestStreak, profile.CurrentStreak);
    }

    // Ленивое «потухание»: пропущен день → на чтении показываем 0,
    // ничего не записывая (§4.5, без ночного джоба).
    public static int EffectiveStreak(ReaderProfile profile, DateOnly today) =>
        profile.LastActiveDate is { } last && today.DayNumber - last.DayNumber <= 1
            ? profile.CurrentStreak
            : 0;

    public static DateOnly TodayFor(string timeZoneId)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
    }
}
