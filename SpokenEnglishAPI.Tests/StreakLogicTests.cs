using FluentAssertions;

namespace SpokenEnglishAPI.Tests;

/// <summary>Tests for streak calculation logic (mirrors sp_userstreak_update SQL).</summary>
public class StreakLogicTests
{
    private static (int streak, int longest) CalculateStreak(DateOnly? lastDate, int currentStreak, int longestStreak)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (lastDate == null)         return (1, Math.Max(1, longestStreak));
        if (lastDate == today)        return (currentStreak, longestStreak);
        if (lastDate == today.AddDays(-1))
        {
            var newStreak = currentStreak + 1;
            return (newStreak, Math.Max(newStreak, longestStreak));
        }
        return (1, longestStreak);  // streak broken
    }

    [Fact] public void FirstActivity_SetsStreakToOne()
    {
        var (streak, longest) = CalculateStreak(null, 0, 0);
        streak.Should().Be(1);
        longest.Should().Be(1);
    }

    [Fact] public void ConsecutiveDay_IncrementsStreak()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var (streak, longest) = CalculateStreak(yesterday, 3, 5);
        streak.Should().Be(4);
        longest.Should().Be(5);
    }

    [Fact] public void ConsecutiveDay_UpdatesLongestWhenExceeded()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var (streak, longest) = CalculateStreak(yesterday, 7, 7);
        streak.Should().Be(8);
        longest.Should().Be(8);
    }

    [Fact] public void SameDayActivity_DoesNotChangeStreak()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (streak, longest) = CalculateStreak(today, 5, 10);
        streak.Should().Be(5);
        longest.Should().Be(10);
    }

    [Fact] public void MissedDay_ResetsStreakToOne_KeepsLongest()
    {
        var threeDaysAgo = DateOnly.FromDateTime(DateTime.Today.AddDays(-3));
        var (streak, longest) = CalculateStreak(threeDaysAgo, 15, 15);
        streak.Should().Be(1);
        longest.Should().Be(15);
    }
}
