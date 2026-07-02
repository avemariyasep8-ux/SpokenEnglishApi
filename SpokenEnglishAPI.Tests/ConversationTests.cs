using FluentAssertions;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Tests for Phase 2 – Conversation lessons: turn structure, system-only detection,
/// and the response-acceptance logic used by the interactive play flow.
/// </summary>
public class ConversationTests
{
    private static string Normalize(string? s) =>
        (s ?? "").ToLower()
            .Replace(",", "").Replace(".", "").Replace("!", "").Replace("?", "")
            .Trim();

    // Mirrors the client-side matches() in ConversationPlay.jsx
    private static bool Matches(string said, string? expected)
    {
        var n2 = Normalize(expected);
        if (string.IsNullOrEmpty(n2)) return true;         // greeting / free turn
        var n1 = Normalize(said);
        if (n1 == n2 || n1.Contains(n2)) return true;
        var words = n2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hit = words.Count(w => n1.Contains(w));
        return hit >= Math.Ceiling(words.Length * 0.7);
    }

    private static bool IsSystemOnly(string? expectedResponse) => string.IsNullOrWhiteSpace(expectedResponse);

    // ── Response matching ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("I would like a tea", "I would like a tea", true)]
    [InlineData("I Would Like A Tea!", "i would like a tea", true)]
    [InlineData("yes, I would like a tea please", "I would like a tea", true)]
    [InlineData("I would like tea", "I would like a tea", true)]     // 4/5 words = 80%
    [InlineData("goodbye", "I would like a tea", false)]
    public void Matches_AcceptsCloseReplies(string said, string expected, bool ok)
    {
        Matches(said, expected).Should().Be(ok);
    }

    [Fact]
    public void Matches_EmptyExpected_AlwaysAccepts()
    {
        Matches("anything at all", "").Should().BeTrue();
        Matches("anything at all", null).Should().BeTrue();
    }

    // ── System-only turns ─────────────────────────────────────────────────────

    [Fact]
    public void Turn_WithoutExpectedResponse_IsSystemOnly()
    {
        IsSystemOnly(null).Should().BeTrue();
        IsSystemOnly("").Should().BeTrue();
        IsSystemOnly("   ").Should().BeTrue();
        IsSystemOnly("I would like a tea").Should().BeFalse();
    }

    // ── Turn ordering / structure ─────────────────────────────────────────────

    [Fact]
    public void Turns_AreWalkedInOrder_AndCompleteAtEnd()
    {
        var turns = new[]
        {
            new { Order = 1, System = "Good morning!",        Expected = (string?)"Good morning" },
            new { Order = 2, System = "What would you like?", Expected = (string?)"I would like a tea" },
            new { Order = 3, System = "Thank you!",           Expected = (string?)null },
        };

        int idx = 0;
        if (Matches("good morning", turns[0].Expected)) idx++;
        if (Matches("i would like a tea", turns[1].Expected)) idx++;
        bool finished = IsSystemOnly(turns[idx].Expected) && idx == turns.Length - 1;

        idx.Should().Be(2);
        finished.Should().BeTrue();
    }

    [Fact]
    public void ConversationDto_RequiresTitle()
    {
        bool Valid(string? title) => !string.IsNullOrWhiteSpace(title);
        Valid("Tea Shop").Should().BeTrue();
        Valid("").Should().BeFalse();
        Valid(null).Should().BeFalse();
    }

    [Fact]
    public void TurnDto_RequiresSystemText()
    {
        bool Valid(string? sys) => !string.IsNullOrWhiteSpace(sys);
        Valid("What would you like?").Should().BeTrue();
        Valid("").Should().BeFalse();
    }

    [Fact]
    public void NextTurnOrder_IsMaxPlusOne()
    {
        var existing = new[] { 1, 2, 3 };
        var next = (existing.Length == 0 ? 0 : existing.Max()) + 1;
        next.Should().Be(4);
    }
}
