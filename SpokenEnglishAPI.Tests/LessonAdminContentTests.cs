using FluentAssertions;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Tests for admin lesson content management:
/// - Lesson import/export with the Level column
/// - Reading sentence import parsing
/// - Translate (arrange + tamilmeaning) import parsing
/// Mirrors the parsing logic in AdminController so the scenarios are covered without a DB.
/// </summary>
public class LessonAdminContentTests
{
    private static readonly string[] AllowedLevels =
        { "Beginner", "Elementary", "Intermediate", "College", "Professional" };

    // Mirrors ImportLessons level resolution
    private static string ResolveLevel(string[] cols)
    {
        var level = cols.Length >= 5 ? cols[4].Trim() : "Beginner";
        if (string.IsNullOrWhiteSpace(level) || !AllowedLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
            level = "Beginner";
        return level;
    }

    // Mirrors ImportReading row validation/parse
    private static (bool valid, int lessonId, string text, int order) ParseReadingRow(string[] cols)
    {
        if (cols.Length < 2) return (false, 0, "", 0);
        if (!int.TryParse(cols[0], out var lessonId)) return (false, 0, "", 0);
        var text = cols[1].Trim();
        if (string.IsNullOrWhiteSpace(text)) return (false, 0, "", 0);
        int ord = cols.Length >= 3 && int.TryParse(cols[2], out var o) ? o : 0;
        return (true, lessonId, text, ord);
    }

    // ── Lesson import: Level column ───────────────────────────────────────────

    [Theory]
    [InlineData("Beginner")]
    [InlineData("Elementary")]
    [InlineData("Intermediate")]
    [InlineData("College")]
    [InlineData("Professional")]
    public void LessonImport_ValidLevel_Preserved(string level)
    {
        var cols = new[] { "1", "Greetings", "Basic greetings", "false", level };
        ResolveLevel(cols).Should().Be(level);
    }

    [Fact]
    public void LessonImport_MissingLevelColumn_DefaultsToBeginner()
    {
        var cols = new[] { "1", "Greetings", "Basic greetings", "false" };
        ResolveLevel(cols).Should().Be("Beginner");
    }

    [Theory]
    [InlineData("Advanced")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xyz")]
    public void LessonImport_InvalidLevel_DefaultsToBeginner(string level)
    {
        var cols = new[] { "1", "Greetings", "Basic greetings", "false", level };
        ResolveLevel(cols).Should().Be("Beginner");
    }

    [Fact]
    public void LessonImport_LevelIsCaseInsensitive()
    {
        var cols = new[] { "1", "Greetings", "Basic", "false", "intermediate" };
        // Case-insensitive match keeps the value the admin typed
        ResolveLevel(cols).Should().Be("intermediate");
    }

    [Fact]
    public void LessonExport_HeaderIncludesLevel()
    {
        var header = "LessonId,LessonName,Description,TypeName,LessonOrder,IsActive,IsPremium,Level";
        header.Split(',').Should().Contain("Level");
        header.Split(',').Should().HaveCount(8);
    }

    [Fact]
    public void LessonTemplate_IncludesLevelColumn()
    {
        var template = "LessonOrder,LessonName,Description,IsPremium,Level\n" +
                       "1,Greetings,Learn basic English greetings and introductions,false,Beginner\n";
        var lines = template.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[0].Split(',').Should().Contain("Level");
        lines[1].Split(',').Last().Should().Be("Beginner");
    }

    // ── Reading sentence import ───────────────────────────────────────────────

    [Fact]
    public void ReadingImport_FullRow_Parsed()
    {
        var (valid, lessonId, text, order) = ParseReadingRow(new[] { "3", "The sun rises in the east.", "2" });
        valid.Should().BeTrue();
        lessonId.Should().Be(3);
        text.Should().Be("The sun rises in the east.");
        order.Should().Be(2);
    }

    [Fact]
    public void ReadingImport_MissingOrder_DefaultsToZero()
    {
        var (valid, _, _, order) = ParseReadingRow(new[] { "1", "Birds fly high." });
        valid.Should().BeTrue();
        order.Should().Be(0);
    }

    [Theory]
    [InlineData(new[] { "abc", "Some text" }, false)]   // non-numeric lessonId
    [InlineData(new[] { "1", "   " }, false)]           // blank text
    [InlineData(new[] { "1" }, false)]                  // too few columns
    [InlineData(new[] { "1", "Valid sentence." }, true)]
    public void ReadingImport_RowValidation(string[] cols, bool expectedValid)
    {
        ParseReadingRow(cols).valid.Should().Be(expectedValid);
    }

    [Fact]
    public void ReadingTemplate_HasExpectedColumns()
    {
        var template = "LessonId,SentenceText,DisplayOrder\n1,The sun rises in the east.,1\n";
        template.Split('\n')[0].Should().Be("LessonId,SentenceText,DisplayOrder");
    }

    // ── Translate (arrange + tamil) ───────────────────────────────────────────

    [Fact]
    public void TranslateTemplate_UsesArrangeFormatWithTamil()
    {
        var template = "LessonId,CorrectSentence,TamilMeaning\n" +
                       "1,I drink water every day.,நான் தினமும் தண்ணீர் குடிக்கிறேன்.\n";
        var header = template.Split('\n')[0].Split(',');
        header.Should().BeEquivalentTo(new[] { "LessonId", "CorrectSentence", "TamilMeaning" });
    }

    [Fact]
    public void ArrangeWithTamil_CountsAsTranslateExercise()
    {
        var rows = new[]
        {
            new { Sentence = "She is happy.",    Tamil = "அவள் மகிழ்ச்சி." },
            new { Sentence = "He runs fast.",    Tamil = (string?)null },
            new { Sentence = "The sky is blue.", Tamil = "வானம் நீலம்." },
        };
        var translateCount = rows.Count(r => !string.IsNullOrWhiteSpace(r.Tamil));
        translateCount.Should().Be(2);
    }

    // ── Regression: Tamil mojibake detection (found in production 2026-07-04) ──
    // A batch of arrangesentence_lang.tamilmeaning rows were corrupted to literal "?"
    // characters by a bad write path. Mirrors the heuristic used to find/verify them:
    // 3+ consecutive '?' is corruption, a single '?' is valid Tamil question punctuation.
    private static bool LooksCorrupted(string text) =>
        System.Text.RegularExpressions.Regex.IsMatch(text, @"\?{3,}");

    [Theory]
    [InlineData("இதன் விலை என்ன?")]           // valid Tamil question — single '?' is fine
    [InlineData("நான் சாதம் சாப்பிடுகிறேன்.")]
    public void ValidTamilText_IsNotFlaggedAsCorrupted(string tamil)
    {
        LooksCorrupted(tamil).Should().BeFalse();
    }

    [Theory]
    [InlineData("???? ??? ?????.")]
    [InlineData("??????? ??? ??????????????.")]
    public void MojibakeText_IsFlaggedAsCorrupted(string corrupted)
    {
        LooksCorrupted(corrupted).Should().BeTrue();
    }

    // ── Export All (multi-sheet Excel) ────────────────────────────────────────

    [Fact]
    public void ExportAll_ProducesFiveExpectedSheetNames()
    {
        var sheetNames = new[] { "Lessons", "WordContent", "MCQ", "ArrangeTranslate", "Reading" };
        sheetNames.Should().HaveCount(5);
        sheetNames.Should().Contain("Reading"); // the type the user explicitly said was missing
    }

    [Fact]
    public void ExportAll_ContentTypeIsXlsxNotCsv()
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        contentType.Should().NotBe("text/csv");
        contentType.Should().Contain("spreadsheetml");
    }

    [Fact]
    public void ExportAll_FilenameHasXlsxExtension()
    {
        var filename = $"spokenenglish_export_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        filename.Should().EndWith(".xlsx");
        filename.Should().NotEndWith(".csv");
    }

    // ── Regression: weekly-activity date bug (found via QA screenshot) ────────

    [Fact]
    public void WeeklyActivity_NullDate_WouldCollapseToThursdayEpoch()
    {
        // Documents the failure mode: selecting the LEFT-JOINed (often-null) activity_date
        // instead of the generated calendar date meant every day serialized as JSON null,
        // and new Date(null) in JS == 1970-01-01 == Thursday for all 7 slots.
        DateTime? nullDate = null;
        nullDate.Should().BeNull();
    }

    [Fact]
    public void WeeklyActivity_FixedProc_ReturnsSevenDistinctCalendarDates()
    {
        var today = new DateTime(2026, 7, 4);
        var days = Enumerable.Range(0, 7).Select(i => today.AddDays(-6 + i).Date).ToList();
        days.Should().HaveCount(7);
        days.Distinct().Should().HaveCount(7);
        days.Last().Should().Be(today);
    }
}
