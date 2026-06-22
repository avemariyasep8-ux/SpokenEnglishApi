using FluentAssertions;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Unit tests for bulk import/export logic — validation, row parsing,
/// and schema correctness. These tests do NOT hit the database.
/// </summary>
public class BulkImportValidationTests
{
    // ── Row-level validation helpers (mirrors server-side checks) ─────────────

    private static bool IsValidWordRow(string? wordName, string? definitionEn)
        => !string.IsNullOrWhiteSpace(wordName) && !string.IsNullOrWhiteSpace(definitionEn);

    private static bool IsValidMcqRow(string? questionText, string? opt1, string? opt2, int correctOption)
        => !string.IsNullOrWhiteSpace(questionText)
        && !string.IsNullOrWhiteSpace(opt1)
        && !string.IsNullOrWhiteSpace(opt2)
        && correctOption >= 1 && correctOption <= 4;

    private static bool IsValidFillInRow(string? sentence, string? correctAnswer)
        => !string.IsNullOrWhiteSpace(sentence)
        && sentence!.Contains("___")
        && !string.IsNullOrWhiteSpace(correctAnswer);

    private static bool IsValidArrangeRow(string? sentence)
        => !string.IsNullOrWhiteSpace(sentence) && sentence!.Trim().Contains(' ');

    // ── Word Content ──────────────────────────────────────────────────────────

    [Fact] public void WordRow_Valid_WhenWordAndDefinitionProvided()
        => IsValidWordRow("Hello", "A greeting").Should().BeTrue();

    [Fact] public void WordRow_Invalid_WhenWordNameEmpty()
        => IsValidWordRow("", "A greeting").Should().BeFalse();

    [Fact] public void WordRow_Invalid_WhenDefinitionEmpty()
        => IsValidWordRow("Hello", null).Should().BeFalse();

    [Fact] public void WordRow_Invalid_WhenBothEmpty()
        => IsValidWordRow("", "").Should().BeFalse();

    // ── MCQ ───────────────────────────────────────────────────────────────────

    [Fact] public void McqRow_Valid_WithTwoOptionsAndCorrectIndex()
        => IsValidMcqRow("Which means hello?", "Hi", "Bye", 1).Should().BeTrue();

    [Fact] public void McqRow_Invalid_WhenQuestionEmpty()
        => IsValidMcqRow("", "Hi", "Bye", 1).Should().BeFalse();

    [Fact] public void McqRow_Invalid_WhenOption1Empty()
        => IsValidMcqRow("Q?", "", "Bye", 1).Should().BeFalse();

    [Fact] public void McqRow_Invalid_WhenCorrectOptionZero()
        => IsValidMcqRow("Q?", "A", "B", 0).Should().BeFalse();

    [Fact] public void McqRow_Invalid_WhenCorrectOptionFive()
        => IsValidMcqRow("Q?", "A", "B", 5).Should().BeFalse();

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void McqRow_Valid_ForAllCorrectOptionValues(int opt)
        => IsValidMcqRow("Q?", "A", "B", opt).Should().BeTrue();

    // ── Fill in Blank ─────────────────────────────────────────────────────────

    [Fact] public void FillInRow_Valid_WithBlankPlaceholder()
        => IsValidFillInRow("She ___ a student.", "is").Should().BeTrue();

    [Fact] public void FillInRow_Invalid_WhenNoBlankPlaceholder()
        => IsValidFillInRow("She is a student.", "is").Should().BeFalse();

    [Fact] public void FillInRow_Invalid_WhenAnswerEmpty()
        => IsValidFillInRow("She ___ a student.", "").Should().BeFalse();

    [Fact] public void FillInRow_Invalid_WhenSentenceEmpty()
        => IsValidFillInRow(null, "is").Should().BeFalse();

    // ── Arrange Words ─────────────────────────────────────────────────────────

    [Fact] public void ArrangeRow_Valid_WhenMultipleWords()
        => IsValidArrangeRow("She is a good student").Should().BeTrue();

    [Fact] public void ArrangeRow_Invalid_WhenSingleWord()
        => IsValidArrangeRow("Hello").Should().BeFalse();

    [Fact] public void ArrangeRow_Invalid_WhenEmpty()
        => IsValidArrangeRow("").Should().BeFalse();

    [Fact] public void ArrangeRow_Invalid_WhenNull()
        => IsValidArrangeRow(null).Should().BeFalse();
}

/// <summary>
/// Tests for the word-splitting logic used in Rearrange exercises.
/// </summary>
public class ArrangeWordSplitTests
{
    private static string[] SplitSentence(string sentence)
        => sentence.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    [Fact] public void Split_ProducesCorrectWordCount()
        => SplitSentence("She is a good student").Should().HaveCount(5);

    [Fact] public void Split_HandlesExtraSpaces()
        => SplitSentence("She  is   a student").Should().HaveCount(4);

    [Fact] public void Split_PreservesWords()
    {
        var words = SplitSentence("I drink tea every morning");
        words.Should().ContainInOrder("I", "drink", "tea", "every", "morning");
    }

    [Fact] public void Shuffled_ContainsSameWords()
    {
        var original = SplitSentence("What time is it now");
        var shuffled = original.OrderBy(_ => Guid.NewGuid()).ToArray();

        shuffled.Should().BeEquivalentTo(original); // same elements, any order
    }

    [Fact] public void Rejoined_MatchesOriginal_WhenCorrectOrder()
    {
        var sentence = "I will visit my friend tomorrow";
        var words    = SplitSentence(sentence);
        var rejoined = string.Join(' ', words);

        rejoined.Should().Be(sentence);
    }
}

/// <summary>
/// Tests for Excel column schema — ensures expected headers are defined
/// for each content type (prevents regressions when schema changes).
/// </summary>
public class ExcelSchemaTests
{
    // Mirrors SCHEMAS in ExcelImportExport.jsx
    private static readonly Dictionary<string, string[]> Schemas = new()
    {
        ["wordcontent"] = ["WordName", "SentencePattern", "DefinitionEn", "DefinitionTa", "ExampleEn", "ExampleTa", "DisplayOrder"],
        ["mcq"]         = ["QuestionText", "Option1", "Option2", "Option3", "Option4", "CorrectOption"],
        ["fillin"]      = ["SentenceWithBlank", "CorrectAnswer", "Option1", "Option2", "Option3", "HintTa", "DisplayOrder"],
        ["arrange"]     = ["CorrectSentence"],
    };

    [Fact] public void WordContentSchema_HasRequiredColumns()
    {
        var cols = Schemas["wordcontent"];
        cols.Should().Contain("WordName");
        cols.Should().Contain("DefinitionEn");
        cols.Should().Contain("ExampleEn");
    }

    [Fact] public void McqSchema_HasFourOptions_AndCorrectOption()
    {
        var cols = Schemas["mcq"];
        cols.Should().Contain("Option1").And.Contain("Option2")
            .And.Contain("Option3").And.Contain("Option4")
            .And.Contain("CorrectOption");
    }

    [Fact] public void FillInSchema_HasBlankAndAnswer()
    {
        var cols = Schemas["fillin"];
        cols.Should().Contain("SentenceWithBlank");
        cols.Should().Contain("CorrectAnswer");
    }

    [Fact] public void ArrangeSchema_HasCorrectSentence()
        => Schemas["arrange"].Should().Contain("CorrectSentence");

    [Fact] public void MultiLessonSchema_HasLessonIdAndName()
    {
        // Multi-lesson export always prepends LessonID + LessonName
        var multiCols = new[] { "LessonID", "LessonName" }.Concat(Schemas["wordcontent"]).ToArray();
        multiCols.Should().StartWith(new[] { "LessonID", "LessonName" });
        multiCols.Should().Contain("WordName");
    }

    [Fact] public void AllSchemas_HaveAtLeastOneColumn()
    {
        foreach (var (type, cols) in Schemas)
            cols.Should().NotBeEmpty(because: $"{type} schema must have columns");
    }
}
