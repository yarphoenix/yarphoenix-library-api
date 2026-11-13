using YarPhoenix.Catalog.Application;
using YarPhoenix.Catalog.Domain;

namespace YarPhoenix.Catalog.Tests;

public sealed class StreakLogicTests
{
    private static readonly DateOnly Today = new(2026, 7, 7);

    [Fact]
    public void Advance_StartsStreakAtOne_ForFirstActivity()
    {
        var profile = new ReaderProfile();

        StreakLogic.Advance(profile, Today);

        Assert.Equal(1, profile.CurrentStreak);
        Assert.Equal(1, profile.LongestStreak);
        Assert.Equal(Today, profile.LastActiveDate);
    }

    [Fact]
    public void Advance_IsIdempotentWithinOneDay()
    {
        var profile = new ReaderProfile { CurrentStreak = 3, LongestStreak = 5, LastActiveDate = Today };

        StreakLogic.Advance(profile, Today);

        Assert.Equal(3, profile.CurrentStreak);
    }

    [Fact]
    public void Advance_Increments_WhenLastActivityWasYesterday()
    {
        var profile = new ReaderProfile
        {
            CurrentStreak = 3,
            LongestStreak = 3,
            LastActiveDate = Today.AddDays(-1),
        };

        StreakLogic.Advance(profile, Today);

        Assert.Equal(4, profile.CurrentStreak);
        Assert.Equal(4, profile.LongestStreak);
    }

    [Fact]
    public void Advance_ResetsToOne_AfterMissedDay_KeepingLongest()
    {
        var profile = new ReaderProfile
        {
            CurrentStreak = 30,
            LongestStreak = 30,
            LastActiveDate = Today.AddDays(-2),
        };

        StreakLogic.Advance(profile, Today);

        Assert.Equal(1, profile.CurrentStreak);
        Assert.Equal(30, profile.LongestStreak);
    }

    [Fact]
    public void EffectiveStreak_IsZero_WhenStreakWentOut_WithoutWriting()
    {
        var profile = new ReaderProfile { CurrentStreak = 10, LastActiveDate = Today.AddDays(-3) };

        Assert.Equal(0, StreakLogic.EffectiveStreak(profile, Today));
        Assert.Equal(10, profile.CurrentStreak);
    }

    [Fact]
    public void EffectiveStreak_KeepsValue_ForTodayAndYesterday()
    {
        Assert.Equal(
            7,
            StreakLogic.EffectiveStreak(
                new ReaderProfile { CurrentStreak = 7, LastActiveDate = Today }, Today));
        Assert.Equal(
            7,
            StreakLogic.EffectiveStreak(
                new ReaderProfile { CurrentStreak = 7, LastActiveDate = Today.AddDays(-1) }, Today));
    }

    [Fact]
    public void TodayFor_FallsBackToUtc_ForUnknownTimeZone()
    {
        var expected = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Equal(expected, StreakLogic.TodayFor("No/Such_Zone"));
    }
}
