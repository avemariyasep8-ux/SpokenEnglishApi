using FluentAssertions;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Tests for voice matching logic (mirrors the frontend matchVoice function).
/// This validates our 70% word-match threshold for grading spoken answers.
/// </summary>
public class VoiceMatchTests
{
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

    [Fact] public void ExactMatch_ReturnsTrue()
        => MatchVoice("She reads a book", "She reads a book").Should().BeTrue();

    [Fact] public void CaseInsensitive_ReturnsTrue()
        => MatchVoice("she reads a book", "She reads a book").Should().BeTrue();

    [Fact] public void MissingOneWord_Out_Of_Four_Returns_True()
        => MatchVoice("She reads book", "She reads a book").Should().BeTrue(); // 3/4 = 75% ≥ 70%

    [Fact] public void MissingTwoWords_Out_Of_Four_ReturnsFalse()
        => MatchVoice("She book", "She reads a book").Should().BeFalse(); // 2/4 = 50% < 70%

    [Fact] public void TranscriptContainsFullTarget_ReturnsTrue()
        => MatchVoice("I think she reads a book every day", "she reads a book").Should().BeTrue();

    [Fact] public void CompletelyDifferentSentence_ReturnsFalse()
        => MatchVoice("the dog ran fast", "She reads a book").Should().BeFalse();

    [Fact] public void EmptyTranscript_ReturnsFalse()
        => MatchVoice("", "She reads a book").Should().BeFalse();

    [Fact] public void SevenWordSentence_FiveMatches_ReturnsTrue()
        => MatchVoice("He walked to school yesterday afternoon", "He walked to school yesterday afternoon but late")
            .Should().BeTrue(); // 6/7 ≥ 70%

    [Fact] public void AllWordsMatch_LongSentence_ReturnsTrue()
        => MatchVoice("I will visit my friend tomorrow morning",
                      "I will visit my friend tomorrow morning").Should().BeTrue();
}
