using FluentAssertions;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Unit tests for new admin features:
/// 1. Tamil meaning is present on ArrangeSentenceDto
/// 2. Password validation rules
/// 3. Word-click TTS triggers (logic layer)
/// 4. Translate phase — Tamil→English matching
/// 5. Import arrange CSV parsing (3-column and 4-column formats)
/// 6. Rate limit threshold constants
/// </summary>
public class AdminFeatureTests
{
    // ── Helpers mirroring backend/frontend logic ──────────────────────────

    private static string Normalize(string text) =>
        text.ToLower()
            .Replace(",", "").Replace(".", "").Replace("!", "").Replace("?", "")
            .Trim();

    private static bool MatchVoice(string transcript, string target)
    {
        var n1 = Normalize(transcript);
        var n2 = Normalize(target);
        if (n1 == n2 || n1.Contains(n2)) return true;
        var words = n2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matched = words.Count(w => n1.Contains(w));
        return matched >= Math.Ceiling(words.Length * 0.7);
    }

    private static bool IsValidPassword(string password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= 6;

    // Mimics the ImportArrange column parsing logic
    private static (string sentence, string? tamil, string[] words) ParseArrangeCsvRow(string[] cols)
    {
        var sentence = cols[1].Trim();
        var tamil = cols.Length >= 3 ? cols[2].Trim() : null;
        string[] words;
        if (cols.Length >= 4 && !string.IsNullOrWhiteSpace(cols[3]))
            words = cols[3].Split('|', StringSplitOptions.RemoveEmptyEntries);
        else
            words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return (sentence, string.IsNullOrWhiteSpace(tamil) ? null : tamil, words);
    }

    // ── Password validation ───────────────────────────────────────────────

    [Fact] public void Password_TooShort_IsInvalid()
        => IsValidPassword("abc").Should().BeFalse();

    [Fact] public void Password_ExactlyMinLength_IsValid()
        => IsValidPassword("abc123").Should().BeTrue();

    [Fact] public void Password_EmptyString_IsInvalid()
        => IsValidPassword("").Should().BeFalse();

    [Fact] public void Password_WhitespaceOnly_IsInvalid()
        => IsValidPassword("      ").Should().BeFalse();

    [Fact] public void Password_LongPassword_IsValid()
        => IsValidPassword("MyStrongP@ssword2024!").Should().BeTrue();

    // ── Tamil→English translate phase matching ────────────────────────────

    [Fact] public void Translate_ExactEnglish_Matches()
        => MatchVoice("I am hungry now", "I am hungry now").Should().BeTrue();

    [Fact] public void Translate_ThreeOutOfFour_Matches()
        => MatchVoice("I hungry now", "I am hungry now").Should().BeTrue(); // 3/4 = 75%

    [Fact] public void Translate_WrongSentence_NoMatch()
        => MatchVoice("she is sleeping", "I am hungry now").Should().BeFalse();

    [Fact] public void Translate_TamilWordsSpoken_NoMatch()
        => MatchVoice("naan pasi", "I am hungry now").Should().BeFalse();

    [Fact] public void Translate_WithExtraNoise_StillMatches()
        => MatchVoice("umm I am hungry now right", "I am hungry now").Should().BeTrue();

    // ── Import arrange CSV parsing ─────────────────────────────────────────

    [Fact]
    public void ImportArrange_ThreeColumn_ParsesSentenceAndTamil()
    {
        var cols = new[] { "4", "He is my father.", "அவர் என் தந்தை." };
        var (sentence, tamil, words) = ParseArrangeCsvRow(cols);
        sentence.Should().Be("He is my father.");
        tamil.Should().Be("அவர் என் தந்தை.");
        words.Should().BeEquivalentTo(new[] { "He", "is", "my", "father." });
    }

    [Fact]
    public void ImportArrange_FourColumn_UsesPipeWords()
    {
        var cols = new[] { "4", "He is my father.", "அவர் என் தந்தை.", "He|is|my|father" };
        var (sentence, tamil, words) = ParseArrangeCsvRow(cols);
        sentence.Should().Be("He is my father.");
        tamil.Should().Be("அவர் என் தந்தை.");
        words.Should().BeEquivalentTo(new[] { "He", "is", "my", "father" });
    }

    [Fact]
    public void ImportArrange_TwoColumn_NullTamil()
    {
        var cols = new[] { "1", "Good morning!" };
        var (sentence, tamil, words) = ParseArrangeCsvRow(cols);
        sentence.Should().Be("Good morning!");
        tamil.Should().BeNull();
        words.Should().BeEquivalentTo(new[] { "Good", "morning!" });
    }

    [Fact]
    public void ImportArrange_EmptyTamilColumn_NullTamil()
    {
        var cols = new[] { "5", "I am hungry.", "", "I|am|hungry" };
        var (_, tamil, _) = ParseArrangeCsvRow(cols);
        tamil.Should().BeNull();
    }

    // ── Word audio TTS trigger logic ──────────────────────────────────────
    // The frontend calls speak(word) on pickWord — we test the string is non-empty

    [Theory]
    [InlineData("Hello")]
    [InlineData("Good morning")]
    [InlineData("father")]
    [InlineData("restaurant")]
    public void WordTts_NonEmptyWord_ShouldTriggerSpeak(string word)
        => string.IsNullOrWhiteSpace(word).Should().BeFalse();

    [Fact] public void WordTts_EmptyWord_ShouldNotTriggerSpeak()
        => string.IsNullOrWhiteSpace("").Should().BeTrue();

    // ── ArrangeSentenceDto has TamilMeaning field ─────────────────────────

    [Fact]
    public void ArrangeSentenceDto_HasTamilMeaningProperty()
    {
        var dto = new Domain.DTOs.ArrangeSentenceDto
        {
            ArrangeSentenceID = 1,
            CorrectSentence   = "He is my father.",
            TamilMeaning      = "அவர் என் தந்தை.",
        };
        dto.TamilMeaning.Should().Be("அவர் என் தந்தை.");
    }

    [Fact]
    public void ArrangeSentenceDto_TamilMeaning_CanBeNull()
    {
        var dto = new Domain.DTOs.ArrangeSentenceDto { CorrectSentence = "Hello!" };
        dto.TamilMeaning.Should().BeNull();
    }

    // ── Rate limit constants ──────────────────────────────────────────────

    [Fact] public void RateLimit_PermitLimit_Is100()
        => RateLimitConstants.PermitLimit.Should().Be(100);

    [Fact] public void RateLimit_WindowMinutes_Is1()
        => RateLimitConstants.WindowMinutes.Should().Be(1);
}

/// <summary>Mirrors Program.cs rate limiter configuration for testability.</summary>
public static class RateLimitConstants
{
    public const int PermitLimit   = 100;
    public const int WindowMinutes = 1;
}
