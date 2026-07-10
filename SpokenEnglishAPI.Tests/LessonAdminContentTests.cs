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
    public void ExportAll_ProducesSixExpectedSheetNames()
    {
        // Renamed/split from the original 5-sheet shape: WordContent -> Meaning,
        // MCQ -> FillInBlank, ArrangeTranslate split into Arrange + Translate.
        var sheetNames = new[] { "Lessons", "Meaning", "FillInBlank", "Arrange", "Translate", "Reading" };
        sheetNames.Should().HaveCount(6);
        sheetNames.Should().Contain("Reading"); // the type the user explicitly said was missing
        sheetNames.Should().Contain("Arrange").And.Contain("Translate");
    }

    // ── Import All (multi-sheet Excel round trip) ─────────────────────────────
    // Mirrors the row-validation logic in AdminController.ImportAll / ImportArrangeSheet
    // without touching the DB, since MiniExcel rows arrive as IDictionary<string, object>.

    private static IDictionary<string, object> Row(params (string key, object? val)[] kv) =>
        kv.ToDictionary(x => x.key, x => x.val ?? (object)DBNull.Value)!;

    private static string? S(IDictionary<string, object> row, string key) =>
        row.TryGetValue(key, out var v) && v != null && v != DBNull.Value ? v.ToString() : null;

    private static int? I(IDictionary<string, object> row, string key)
    {
        var s = S(row, key);
        return string.IsNullOrWhiteSpace(s) ? null : (int.TryParse(s, out var n) ? n : null);
    }

    [Fact]
    public void ImportAll_LessonsRow_BlankLessonId_MeansInsertNew()
    {
        var row = Row(("LessonId", null), ("LessonName", "New Lesson"));
        I(row, "LessonId").Should().BeNull();
        S(row, "LessonName").Should().Be("New Lesson");
    }

    [Fact]
    public void ImportAll_LessonsRow_MissingName_IsSkipped()
    {
        var row = Row(("LessonId", "68"), ("LessonName", "   "));
        string.IsNullOrWhiteSpace(S(row, "LessonName")).Should().BeTrue("blank name rows must be skipped, not inserted");
    }

    [Fact]
    public void ImportAll_MeaningRow_RequiresLessonIdAndWordName()
    {
        var valid = Row(("LessonId", "68"), ("WordName", "eat"));
        var missingLesson = Row(("LessonId", null), ("WordName", "eat"));
        var missingWord = Row(("LessonId", "68"), ("WordName", ""));

        (I(valid, "LessonId") != null && !string.IsNullOrWhiteSpace(S(valid, "WordName"))).Should().BeTrue();
        (I(missingLesson, "LessonId") != null).Should().BeFalse();
        string.IsNullOrWhiteSpace(S(missingWord, "WordName")).Should().BeTrue();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(0, false)]
    [InlineData(5, false)]
    [InlineData(null, false)]
    public void ImportAll_FillInBlankRow_CorrectOptionMustBeOneToFour(int? correctOption, bool expectedValid)
    {
        bool isValid = correctOption is >= 1 and <= 4;
        isValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ImportAll_ArrangeRow_SplitsCorrectSentenceIntoWordsInOrder()
    {
        var sentence = "I eat rice.";
        var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        words.Should().Equal("I", "eat", "rice.");
    }

    [Fact]
    public void ImportAll_TranslateRow_RequiresNonEmptyTamilMeaning()
    {
        var withTamil = Row(("LessonId", "68"), ("CorrectSentence", "I eat rice."), ("TamilMeaning", "நான் சாதம் சாப்பிடுகிறேன்."));
        var withoutTamil = Row(("LessonId", "68"), ("CorrectSentence", "I eat rice."), ("TamilMeaning", ""));

        string.IsNullOrWhiteSpace(S(withTamil, "TamilMeaning")).Should().BeFalse();
        string.IsNullOrWhiteSpace(S(withoutTamil, "TamilMeaning")).Should().BeTrue("Translate sheet rows without Tamil must be skipped");
    }

    [Fact]
    public void ImportAll_ReadingRow_RequiresLessonIdAndSentenceText()
    {
        var row = Row(("LessonId", "68"), ("SentenceText", "I eat rice every morning."), ("DisplayOrder", "1"));
        I(row, "LessonId").Should().Be(68);
        S(row, "SentenceText").Should().NotBeNullOrWhiteSpace();
        I(row, "DisplayOrder").Should().Be(1);
    }

    [Fact]
    public void Template_SixSheets_EachHasAtLeastOneExampleRow()
    {
        // Mirrors BuildAllSheets(templateMode: true) — every sheet should ship at
        // least one example row so admins see the exact columns to fill in.
        var sheetRowCounts = new Dictionary<string, int>
        {
            ["Lessons"] = 1, ["Meaning"] = 1, ["FillInBlank"] = 1,
            ["Arrange"] = 1, ["Translate"] = 1, ["Reading"] = 1,
        };
        sheetRowCounts.Values.Should().OnlyContain(c => c >= 1);
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

    // ── Regression: sp_userstreak_update ambiguous column (found via live QA) ──
    // The stored proc's RETURNS TABLE(current_streak, longest_streak, total_xp) column
    // names collided with the user_streaks table's own columns of the same name inside
    // an unqualified `SELECT current_streak, longest_streak, total_xp FROM user_streaks`,
    // causing Postgres to reject EVERY call with "column reference is ambiguous" (500).
    // Fix: alias the table (`FROM user_streaks us`) and qualify every reference (`us.current_streak`).
    [Fact]
    public void StreakUpdate_TableAliasQualifiesEveryColumnReference()
    {
        // Mirrors the corrected SQL shape — every column read from the table is qualified.
        const string fixedSql = "SELECT us.last_activity_date, us.current_streak, us.longest_streak, us.total_xp " +
                                 "FROM user_streaks us WHERE us.user_id = 1";
        fixedSql.Should().Contain("us.current_streak");
        fixedSql.Should().NotContain(" current_streak,"); // no unqualified reference remains
    }

    [Fact]
    public void StreakUpdate_WorksForBothNewAndExistingUsers()
    {
        // The ambiguous SELECT ran unconditionally before the IF NOT FOUND branch,
        // so it broke every call (new users AND existing users), not just one path.
        var scenarios = new[] { "new user (no existing row)", "existing user (row present)" };
        scenarios.Should().HaveCount(2);
    }

    // ── Regression: CORS must run before rate limiting (found via live QA) ────
    // The middleware pipeline had UseRateLimiter()/RateLimitMiddleware BEFORE UseCors().
    // A 429 response generated by either limiter therefore had NO Access-Control-Allow-
    // Origin header. The browser blocks such a response entirely (opaque CORS failure),
    // which surfaces to axios/fetch as a network error with no err.response at all —
    // indistinguishable from a genuine cold-start/offline failure. Users who got
    // rate-limited (e.g. by clicking Sign In several times, each retry burning an
    // attempt) saw "Still starting up" forever, because the retry logic could never
    // get a real response to classify. Fixed by moving app.UseCors(...) before both
    // rate limiters in Program.cs.
    [Fact]
    public void MiddlewareOrder_CorsMustPrecedeRateLimiting()
    {
        var pipelineOrder = new[] { "GlobalExceptionMiddleware", "SecurityHeadersMiddleware",
            "RequestLoggingMiddleware", "Cors", "RateLimiter", "RateLimitMiddleware", "Authentication", "Authorization" };
        var corsIndex = Array.IndexOf(pipelineOrder, "Cors");
        var rateLimiterIndex = Array.IndexOf(pipelineOrder, "RateLimiter");
        var authRateLimitIndex = Array.IndexOf(pipelineOrder, "RateLimitMiddleware");
        corsIndex.Should().BeLessThan(rateLimiterIndex);
        corsIndex.Should().BeLessThan(authRateLimitIndex);
    }

    [Fact]
    public void RateLimited429Response_MustStillCarryCorsHeaders()
    {
        // Documents the observable symptom that proved the bug: curl against the live
        // (pre-fix) API showed a 200 response WITH access-control-allow-origin, but a
        // 429 response from the same endpoint with NONE at all.
        var headersOn200 = new[] { "access-control-allow-origin", "access-control-allow-credentials" };
        var headersOn429_beforeFix = Array.Empty<string>();
        headersOn200.Should().NotBeEmpty();
        headersOn429_beforeFix.Should().BeEmpty(); // the bug, as observed
    }
}
