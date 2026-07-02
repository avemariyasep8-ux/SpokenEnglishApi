using FluentAssertions;
using SpokenEnglishAPI.Controllers;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Tests for Phase 1 – Learning Packages:
/// level→package mapping, category validation, and progress percentage.
/// </summary>
public class PackageTests
{
    // Mirrors PackageController.AssignLesson / AdminController category guard
    private static string ResolveCategory(string? category)
    {
        var allowed = new[] { "Grammar", "Vocabulary", "Conversation" };
        return allowed.Contains(category, StringComparer.OrdinalIgnoreCase) ? category! : "Grammar";
    }

    private static int Percent(int completed, int total)
        => total > 0 ? (int)Math.Round(completed * 100.0 / total) : 0;

    // ── Level → Package mapping ───────────────────────────────────────────────

    [Theory]
    [InlineData("Beginner", "Beginner")]
    [InlineData("Elementary", "Beginner")]
    [InlineData("Intermediate", "Intermediate")]
    [InlineData("Advanced", "Advanced")]     // 3-level registration value
    [InlineData("College", "Advanced")]
    [InlineData("Professional", "Advanced")]
    public void LevelToPackageLevel_MapsCorrectly(string userLevel, string expected)
    {
        PackageController.LevelToPackageLevel(userLevel).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown")]
    public void LevelToPackageLevel_UnknownOrNull_DefaultsToBeginner(string? level)
    {
        PackageController.LevelToPackageLevel(level).Should().Be("Beginner");
    }

    [Fact]
    public void EveryUserLevel_MapsToOneOfThreePackages()
    {
        var packages = new[] { "Beginner", "Intermediate", "Advanced" };
        foreach (var lvl in new[] { "Beginner", "Elementary", "Intermediate", "College", "Professional" })
            packages.Should().Contain(PackageController.LevelToPackageLevel(lvl));
    }

    // ── Category validation ───────────────────────────────────────────────────

    [Theory]
    [InlineData("Grammar")]
    [InlineData("Vocabulary")]
    [InlineData("Conversation")]
    public void ResolveCategory_ValidCategory_Preserved(string cat)
    {
        ResolveCategory(cat).Should().Be(cat);
    }

    [Theory]
    [InlineData("grammar", "grammar")]      // case-insensitive match keeps input
    [InlineData("Speaking", "Grammar")]     // invalid → default
    [InlineData(null, "Grammar")]
    [InlineData("", "Grammar")]
    public void ResolveCategory_InvalidOrNull_DefaultsToGrammar(string? input, string expected)
    {
        ResolveCategory(input).Should().Be(expected);
    }

    // ── Progress percentage ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, 0)]     // no lessons
    [InlineData(0, 10, 0)]    // none done
    [InlineData(5, 10, 50)]
    [InlineData(10, 10, 100)]
    [InlineData(1, 3, 33)]    // rounds
    [InlineData(2, 3, 67)]    // rounds
    public void Percent_ComputesRounded(int completed, int total, int expected)
    {
        Percent(completed, total).Should().Be(expected);
    }

    [Fact]
    public void PackageProgress_CategoryBreakdown_SumsToOverall()
    {
        var categories = new[]
        {
            new { Category = "Grammar",      Total = 4, Completed = 4 },
            new { Category = "Vocabulary",   Total = 3, Completed = 1 },
            new { Category = "Conversation", Total = 3, Completed = 0 },
        };
        var total = categories.Sum(c => c.Total);
        var completed = categories.Sum(c => c.Completed);
        total.Should().Be(10);
        completed.Should().Be(5);
        Percent(completed, total).Should().Be(50);
    }

    // ── Package assignment on registration ────────────────────────────────────

    [Fact]
    public void Registration_BeginnerUser_GetsBeginnerPackage()
    {
        // Simulate the UserService flow: level → package level → lookup
        var packages = new[]
        {
            new { Id = 1, Level = "Beginner" },
            new { Id = 2, Level = "Intermediate" },
            new { Id = 3, Level = "Advanced" },
        };
        var userLevel = "Elementary";
        var packageLevel = PackageController.LevelToPackageLevel(userLevel);
        var assigned = packages.First(p => p.Level == packageLevel);
        assigned.Id.Should().Be(1);
    }

    [Fact]
    public void Registration_ProfessionalUser_GetsAdvancedPackage()
    {
        var packages = new[]
        {
            new { Id = 1, Level = "Beginner" },
            new { Id = 2, Level = "Intermediate" },
            new { Id = 3, Level = "Advanced" },
        };
        var packageLevel = PackageController.LevelToPackageLevel("Professional");
        packages.First(p => p.Level == packageLevel).Id.Should().Be(3);
    }
}
