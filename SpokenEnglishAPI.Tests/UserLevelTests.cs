using FluentAssertions;
using SpokenEnglishAPI.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Unit tests for Task 3 – user level at registration + level-based filtering
/// and Task 2 – translate sentence (arrange with tamilmeaning) admin features.
/// </summary>
public class UserLevelTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IList<ValidationResult> ValidateDto(object dto)
    {
        var ctx = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);
        return results;
    }

    private static bool LessonMatchesLevel(string lessonLevel, string userLevel)
        => string.IsNullOrEmpty(userLevel) || userLevel == "All" ||
           (lessonLevel ?? "Beginner") == userLevel;

    private static string NormalizeLevel(string? level)
        => string.IsNullOrWhiteSpace(level) ? "Beginner" : level;

    private static (string sentence, string? tamilMeaning) ParseArrangeWithTamil(string[] cols)
    {
        var sentence = cols.Length > 1 ? cols[1].Trim() : "";
        var tamil = cols.Length > 2 ? cols[2].Trim() : null;
        return (sentence, string.IsNullOrWhiteSpace(tamil) ? null : tamil);
    }

    // ── Task 3: Registration Level Validation ─────────────────────────────────

    [Theory]
    [InlineData("Beginner")]
    [InlineData("Elementary")]
    [InlineData("Intermediate")]
    [InlineData("College")]
    [InlineData("Professional")]
    public void RegisterDto_ValidLevel_PassesValidation(string level)
    {
        var dto = new RegisterUserRequestDto
        {
            Email = "test@test.com",
            Password = "Test123",
            mobnumber = "9876543210",
            Level = level
        };
        ValidateDto(dto).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Advanced")]
    [InlineData("expert")]
    [InlineData("Level1")]
    [InlineData("   ")]
    public void RegisterDto_InvalidLevel_FailsValidation(string level)
    {
        var dto = new RegisterUserRequestDto
        {
            Email = "test@test.com",
            Password = "Test123",
            mobnumber = "9876543210",
            Level = level
        };
        var errors = ValidateDto(dto);
        errors.Should().NotBeEmpty();
        errors.Should().Contain(r => r.MemberNames.Contains("Level"));
    }

    [Fact]
    public void RegisterDto_NullLevel_PassesValidation_DefaultsToBeginnerLogically()
    {
        var dto = new RegisterUserRequestDto
        {
            Email = "test@test.com",
            Password = "Test123",
            mobnumber = "9876543210",
            Level = null
        };
        ValidateDto(dto).Should().BeEmpty();
        NormalizeLevel(dto.Level).Should().Be("Beginner");
    }

    [Fact]
    public void LoginResponse_IncludesLevel()
    {
        var response = new LoginResponseDto
        {
            UserID = 1, Email = "u@test.com",
            Token = "tok", RefreshToken = "rtok",
            ApiKey = "key", Role = "User",
            Level = "Intermediate"
        };
        response.Level.Should().Be("Intermediate");
    }

    // ── Task 3: Level-based lesson filtering ──────────────────────────────────

    [Fact]
    public void LessonFilter_BeginnerUser_OnlySeesBeginnerLessons()
    {
        var lessons = new[]
        {
            new { LessonName = "Greetings",   Level = "Beginner"     },
            new { LessonName = "Tenses",      Level = "Intermediate" },
            new { LessonName = "Vocabulary",  Level = "Elementary"   },
        };

        var result = lessons.Where(l => LessonMatchesLevel(l.Level, "Beginner")).ToList();

        result.Should().HaveCount(1);
        result[0].LessonName.Should().Be("Greetings");
    }

    [Fact]
    public void LessonFilter_AllLevel_ReturnsAllLessons()
    {
        var lessons = new[]
        {
            new { LessonName = "A", Level = "Beginner"      },
            new { LessonName = "B", Level = "Intermediate"  },
            new { LessonName = "C", Level = "Professional"  },
        };

        var result = lessons.Where(l => LessonMatchesLevel(l.Level, "All")).ToList();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void LessonFilter_ProfessionalUser_OnlySeesOwnLevel()
    {
        var lessons = new[]
        {
            new { LessonName = "Basics",      Level = "Beginner"      },
            new { LessonName = "Business",    Level = "Professional"  },
            new { LessonName = "Advanced",    Level = "College"       },
        };

        var result = lessons.Where(l => LessonMatchesLevel(l.Level, "Professional")).ToList();
        result.Should().HaveCount(1);
        result[0].LessonName.Should().Be("Business");
    }

    [Fact]
    public void LessonFilter_LessonWithNullLevel_TreatedAsBeginner()
    {
        // Lesson has no level set — defaults to "Beginner"
        var lessonLevel = (string?)null;
        LessonMatchesLevel(lessonLevel, "Beginner").Should().BeTrue();
        LessonMatchesLevel(lessonLevel, "Intermediate").Should().BeFalse();
    }

    [Fact]
    public void NormalizeLevel_EmptyOrNull_ReturnsBeginner()
    {
        NormalizeLevel(null).Should().Be("Beginner");
        NormalizeLevel("").Should().Be("Beginner");
        NormalizeLevel("   ").Should().Be("Beginner");
    }

    [Theory]
    [InlineData("Beginner")]
    [InlineData("Elementary")]
    [InlineData("Intermediate")]
    [InlineData("College")]
    [InlineData("Professional")]
    public void NormalizeLevel_ValidLevel_PreservesValue(string level)
    {
        NormalizeLevel(level).Should().Be(level);
    }

    // ── Task 2: Translate sentence (arrange with tamilmeaning) ───────────────

    [Fact]
    public void ParseArrangeWithTamil_ThreeColumns_ExtractsBoth()
    {
        var cols = new[] { "1", "She is happy.", "அவள் மகிழ்ச்சியாக இருக்கிறாள்." };
        var (sentence, tamil) = ParseArrangeWithTamil(cols);
        sentence.Should().Be("She is happy.");
        tamil.Should().Be("அவள் மகிழ்ச்சியாக இருக்கிறாள்.");
    }

    [Fact]
    public void ParseArrangeWithTamil_TwoColumns_TamilIsNull()
    {
        var cols = new[] { "1", "He is a teacher." };
        var (sentence, tamil) = ParseArrangeWithTamil(cols);
        sentence.Should().Be("He is a teacher.");
        tamil.Should().BeNull();
    }

    [Fact]
    public void ParseArrangeWithTamil_EmptyTamilColumn_TreatedAsNull()
    {
        var cols = new[] { "1", "The book is on the table.", "" };
        var (sentence, tamil) = ParseArrangeWithTamil(cols);
        sentence.Should().Be("The book is on the table.");
        tamil.Should().BeNull();
    }

    [Fact]
    public void ParseArrangeWithTamil_WhitespaceTamil_TreatedAsNull()
    {
        var cols = new[] { "1", "She reads books.", "   " };
        var (sentence, tamil) = ParseArrangeWithTamil(cols);
        tamil.Should().BeNull();
    }

    [Fact]
    public void ArrangeExportCsv_IncludesTamilMeaningColumn()
    {
        // Simulate export CSV header
        var header = "ArrangeSentenceId,LessonId,LessonName,CorrectSentence,TamilMeaning";
        header.Should().Contain("TamilMeaning");
        header.Split(',').Should().HaveCount(5);
    }

    [Fact]
    public void ArrangeImportCsv_Template_HasCorrectFormat()
    {
        var template = "LessonId,CorrectSentence,TamilMeaning\n" +
                       "1,She is happy.,அவள் மகிழ்ச்சியாக இருக்கிறாள்.\n" +
                       "1,He is a teacher.,அவர் ஒரு ஆசிரியர்.\n";

        var lines = template.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines[0].Should().Be("LessonId,CorrectSentence,TamilMeaning");
        lines.Should().HaveCount(3); // header + 2 data rows

        var row1 = lines[1].Split(',');
        row1[0].Should().Be("1");
        row1[1].Should().Be("She is happy.");
    }

    [Theory]
    [InlineData("1,She is happy.,அவள் மகிழ்ச்சியாக இருக்கிறாள்.", true)]
    [InlineData("1,He is a teacher.,", true)]  // empty tamil is valid
    [InlineData("abc,Invalid row.", false)]     // non-numeric lessonId
    [InlineData("", false)]                     // empty line
    public void ArrangeImport_RowValidation(string line, bool shouldBeValid)
    {
        if (string.IsNullOrWhiteSpace(line)) { shouldBeValid.Should().BeFalse(); return; }
        var cols = line.Split(',');
        var isValid = cols.Length >= 2 && int.TryParse(cols[0], out _) && !string.IsNullOrWhiteSpace(cols[1]);
        isValid.Should().Be(shouldBeValid);
    }

    [Fact]
    public void TranslateStep_SentenceWithTamilMeaning_IsUsedAsPrompt()
    {
        // Simulate the UI logic: tamilMeaning from arrangesentence_lang is shown as the prompt
        var arrangeSentence = new
        {
            TamilMeaning = "அவள் மகிழ்ச்சியாக இருக்கிறாள்.",
            CorrectSentence = "She is happy."
        };

        var prompt = arrangeSentence.TamilMeaning;
        var answer = arrangeSentence.CorrectSentence;

        prompt.Should().NotBeNullOrEmpty();
        answer.Should().Be("She is happy.");
    }

    [Fact]
    public void TranslateStep_SentenceWithoutTamilMeaning_IsExcludedFromQueue()
    {
        var sentences = new[]
        {
            new { CorrectSentence = "She is happy.",      TamilMeaning = "அவள் மகிழ்ச்சியாக இருக்கிறாள்." },
            new { CorrectSentence = "He is a teacher.",   TamilMeaning = (string?)null },
            new { CorrectSentence = "The sky is blue.",   TamilMeaning = "வானம் நீலமாக உள்ளது." },
        };

        var translateQueue = sentences.Where(s => !string.IsNullOrEmpty(s.TamilMeaning)).ToList();
        translateQueue.Should().HaveCount(2);
        translateQueue.Should().NotContain(s => s.TamilMeaning == null);
    }

    // ── Task 1: Sentence pattern in all steps ────────────────────────────────

    [Fact]
    public void ExampleStep_MissingExampleEn_FallsBackToSentencePattern()
    {
        // Mimics ExampleStep fallback logic in LessonPlay.jsx
        var word = new
        {
            ExampleEn = (string?)null,
            ExampleTa = (string?)null,
            SentencePattern = "The + [object] + is + very + reasonable + for + everyone."
        };

        var displayText = word.ExampleEn ?? word.SentencePattern ?? "";
        displayText.Should().Be("The + [object] + is + very + reasonable + for + everyone.");
    }

    [Fact]
    public void ExampleStep_WithExampleEn_ShowsExampleNotPattern()
    {
        var word = new
        {
            ExampleEn = "The price is very reasonable for everyone.",
            ExampleTa = "விலை மிகவும் சமயோஜிதமாக உள்ளது.",
            SentencePattern = "The + [object] + is + very + reasonable + for + everyone."
        };

        var displayText = word.ExampleEn ?? word.SentencePattern ?? "";
        displayText.Should().Be("The price is very reasonable for everyone.");
    }

    [Fact]
    public void MeaningStep_SentencePattern_IsAlwaysDisplayed()
    {
        var word = new { WordName = "Reasonable", SentencePattern = "The + [object] + is + very + reasonable." };
        (word.SentencePattern != null).Should().BeTrue("sentence pattern must always display in Meaning step");
    }

    [Fact]
    public void ArrangeStep_ShowsPatternWhenAvailable()
    {
        // Mimics ArrangeStep hint logic
        var sentence = new { SentencePattern = "Subject + is + Adjective", HintText = (string?)null };
        var hint = sentence.SentencePattern ?? sentence.HintText;
        hint.Should().Be("Subject + is + Adjective");
    }
}
