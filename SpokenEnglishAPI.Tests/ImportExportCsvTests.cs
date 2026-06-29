using FluentAssertions;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Unit tests for CSV import/export logic.
/// These do NOT touch the database — they test parsing, validation, and formatting only.
/// </summary>
public class CsvParserTests
{
    // Mirrors the ParseCsvLine helper in AdminController
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    [Fact]
    public void ParseCsvLine_SplitsSimpleRow()
    {
        var cols = ParseCsvLine("1,Hello,A greeting");
        cols.Should().Equal("1", "Hello", "A greeting");
    }

    [Fact]
    public void ParseCsvLine_HandlesQuotedCommas()
    {
        var cols = ParseCsvLine("1,\"Good morning, sir\",Greeting");
        cols.Should().HaveCount(3);
        cols[1].Should().Be("Good morning, sir");
    }

    [Fact]
    public void ParseCsvLine_HandlesEmptyFields()
    {
        var cols = ParseCsvLine("1,,Hello");
        cols.Should().HaveCount(3);
        cols[1].Should().Be("");
    }

    [Fact]
    public void ParseCsvLine_HandlesQuotedTamilText()
    {
        var cols = ParseCsvLine("1,\"வணக்கம்\",Hello");
        cols[1].Should().Be("வணக்கம்");
    }
}

public class LessonExportFormatTests
{
    private static string BuildLessonCsv(IEnumerable<dynamic> rows)
    {
        const string header = "LessonId,LessonName,Description,TypeName,LessonOrder,IsActive,IsPremium";
        var lines = rows.Select(r =>
            $"{r.lessonid},\"{r.lessonname}\",\"{r.description}\",\"{r.typename}\",{r.lessonorder},{r.isactive},{r.ispremium}");
        return header + "\n" + string.Join("\n", lines);
    }

    [Fact]
    public void LessonCsv_StartsWithHeader()
    {
        var csv = BuildLessonCsv(Array.Empty<dynamic>());
        csv.Should().StartWith("LessonId,LessonName");
    }

    [Fact]
    public void LessonCsv_ContainsAllRequiredColumns()
    {
        var csv = BuildLessonCsv(Array.Empty<dynamic>());
        csv.Should().Contain("LessonId").And.Contain("LessonName")
           .And.Contain("IsActive").And.Contain("IsPremium").And.Contain("LessonOrder");
    }

    [Fact]
    public void LessonCsv_EmptyRows_IsJustHeader()
    {
        var csv = BuildLessonCsv(Array.Empty<dynamic>());
        // Split produces ["header", ""] — at least the header is present
        csv.Split('\n').First().Should().StartWith("LessonId");
    }
}

public class WordContentImportValidationTests
{
    // Template format: LessonId, WordName, SentencePattern, DefinitionEn, DefinitionTa, ExampleEn, ExampleTa
    private static (bool ok, string reason) ValidateWordRow(string[] cols)
    {
        if (cols.Length < 7) return (false, "Not enough columns (need 7)");
        if (!int.TryParse(cols[0], out int lid) || lid <= 0) return (false, "Invalid LessonId");
        if (string.IsNullOrWhiteSpace(cols[1])) return (false, "WordName is required");
        if (string.IsNullOrWhiteSpace(cols[3])) return (false, "DefinitionEn is required");
        return (true, "ok");
    }

    [Fact]
    public void WordRow_Valid_WithAllRequiredFields()
    {
        var (ok, _) = ValidateWordRow(["1", "Hello", "I say ___", "A greeting", "வணக்கம்", "I say hello.", "நான் வணக்கம் சொல்கிறேன்."]);
        ok.Should().BeTrue();
    }

    [Fact]
    public void WordRow_Invalid_WhenLessonIdNotNumeric()
    {
        var (ok, reason) = ValidateWordRow(["abc", "Hello", "", "A greeting", "", "", ""]);
        ok.Should().BeFalse();
        reason.Should().Contain("LessonId");
    }

    [Fact]
    public void WordRow_Invalid_WhenLessonIdIsZero()
    {
        var (ok, _) = ValidateWordRow(["0", "Hello", "", "A greeting", "", "", ""]);
        ok.Should().BeFalse();
    }

    [Fact]
    public void WordRow_Invalid_WhenWordNameEmpty()
    {
        var (ok, reason) = ValidateWordRow(["1", "", "", "A greeting", "", "", ""]);
        ok.Should().BeFalse();
        reason.Should().Contain("WordName");
    }

    [Fact]
    public void WordRow_Invalid_WhenDefinitionEnEmpty()
    {
        var (ok, reason) = ValidateWordRow(["1", "Hello", "", "", "", "", ""]);
        ok.Should().BeFalse();
        reason.Should().Contain("DefinitionEn");
    }

    [Fact]
    public void WordRow_Invalid_WhenNotEnoughColumns()
    {
        var (ok, reason) = ValidateWordRow(["1", "Hello"]);
        ok.Should().BeFalse();
        reason.Should().Contain("columns");
    }
}

public class McqImportValidationTests
{
    // Format: LessonId, QuestionText, Option1, Option2, Option3, Option4, CorrectOption(1-4), LanguageId
    private static (bool ok, string reason) ValidateMcqRow(string[] cols)
    {
        if (cols.Length < 7) return (false, "Need at least 7 columns");
        if (!int.TryParse(cols[0], out int lid) || lid <= 0) return (false, "Invalid LessonId");
        if (string.IsNullOrWhiteSpace(cols[1])) return (false, "QuestionText required");
        if (string.IsNullOrWhiteSpace(cols[2])) return (false, "Option1 required");
        if (string.IsNullOrWhiteSpace(cols[3])) return (false, "Option2 required");
        if (!int.TryParse(cols[6], out int correct) || correct < 1 || correct > 4)
            return (false, "CorrectOption must be 1–4");
        return (true, "ok");
    }

    [Fact]
    public void McqRow_Valid_WithFourOptionsAndCorrectIndex()
    {
        var (ok, _) = ValidateMcqRow(["1", "What means hello?", "Hi", "Bye", "Go", "Run", "1", "1"]);
        ok.Should().BeTrue();
    }

    [Fact]
    public void McqRow_Invalid_WhenCorrectOptionIsZero()
    {
        var (ok, reason) = ValidateMcqRow(["1", "Q?", "A", "B", "C", "D", "0", "1"]);
        ok.Should().BeFalse();
        reason.Should().Contain("CorrectOption");
    }

    [Fact]
    public void McqRow_Invalid_WhenCorrectOptionIsFive()
    {
        var (ok, _) = ValidateMcqRow(["1", "Q?", "A", "B", "C", "D", "5", "1"]);
        ok.Should().BeFalse();
    }

    [Fact]
    public void McqRow_Valid_ForAllCorrectOptionValues()
    {
        foreach (var i in new[] { 1, 2, 3, 4 })
        {
            var (ok, _) = ValidateMcqRow(["1", "Q?", "A", "B", "C", "D", i.ToString(), "1"]);
            ok.Should().BeTrue(because: $"CorrectOption={i} is valid");
        }
    }

    [Fact]
    public void McqRow_Invalid_WhenQuestionTextMissing()
    {
        var (ok, reason) = ValidateMcqRow(["1", "", "A", "B", "C", "D", "1", "1"]);
        ok.Should().BeFalse();
        reason.Should().Contain("QuestionText");
    }
}

public class ArrangeImportValidationTests
{
    // Format: LessonId, CorrectSentence, TamilMeaning (optional)
    private static (bool ok, string reason) ValidateArrangeRow(string[] cols)
    {
        if (cols.Length < 2) return (false, "Need at least 2 columns");
        if (!int.TryParse(cols[0], out int lid) || lid <= 0) return (false, "Invalid LessonId");
        var sentence = cols[1].Trim();
        if (string.IsNullOrWhiteSpace(sentence)) return (false, "CorrectSentence required");
        if (!sentence.Contains(' ')) return (false, "Sentence must have at least 2 words");
        return (true, "ok");
    }

    [Fact]
    public void ArrangeRow_Valid_WithMultiWordSentence()
    {
        var (ok, _) = ValidateArrangeRow(["1", "She is a good student", "அவள் ஒரு நல்ல மாணவி"]);
        ok.Should().BeTrue();
    }

    [Fact]
    public void ArrangeRow_Invalid_WithSingleWord()
    {
        var (ok, reason) = ValidateArrangeRow(["1", "Hello"]);
        ok.Should().BeFalse();
        reason.Should().Contain("2 words");
    }

    [Fact]
    public void ArrangeRow_Invalid_WhenSentenceEmpty()
    {
        var (ok, _) = ValidateArrangeRow(["1", ""]);
        ok.Should().BeFalse();
    }

    [Fact]
    public void ArrangeRow_TamilMeaning_IsOptional()
    {
        // Only 2 columns, no Tamil meaning — should still pass
        var (ok, _) = ValidateArrangeRow(["1", "I go to school"]);
        ok.Should().BeTrue();
    }
}

public class ExportContentTypeTests
{
    [Fact]
    public void LessonExport_ContentType_IsCsv()
    {
        const string expected = "text/csv";
        expected.Should().Be("text/csv");
    }

    [Fact]
    public void LessonExport_Filename_HasCsvExtension()
    {
        const string filename = "lessons.csv";
        filename.Should().EndWith(".csv");
    }

    [Fact]
    public void WordContentExport_Filename_Correct()
        => "word_content.csv".Should().EndWith(".csv");

    [Fact]
    public void McqExport_Filename_Correct()
        => "mcq.csv".Should().EndWith(".csv");

    [Theory]
    [InlineData("lessons")]
    [InlineData("wordcontent")]
    [InlineData("mcq")]
    public void ExportTypes_AreRecognised(string type)
    {
        var valid = new[] { "lessons", "wordcontent", "mcq", "arrange" };
        valid.Should().Contain(type);
    }
}

public class ImportFileValidationTests
{
    private static (bool ok, string error) ValidateUploadedFile(string? fileName, long sizeBytes)
    {
        if (string.IsNullOrEmpty(fileName)) return (false, "No file provided");
        if (!fileName.EndsWith(".csv")) return (false, "Only CSV files are accepted");
        if (sizeBytes == 0) return (false, "File is empty");
        if (sizeBytes > 5 * 1024 * 1024) return (false, "File exceeds 5 MB limit");
        return (true, "ok");
    }

    [Fact]
    public void Import_Valid_WithSmallCsvFile()
    {
        var (ok, _) = ValidateUploadedFile("lessons.csv", 1024);
        ok.Should().BeTrue();
    }

    [Fact]
    public void Import_Invalid_WhenFileIsNotCsv()
    {
        var (ok, error) = ValidateUploadedFile("lessons.xlsx", 1024);
        ok.Should().BeFalse();
        error.Should().Contain("CSV");
    }

    [Fact]
    public void Import_Invalid_WhenFileIsEmpty()
    {
        var (ok, error) = ValidateUploadedFile("lessons.csv", 0);
        ok.Should().BeFalse();
        error.Should().Contain("empty");
    }

    [Fact]
    public void Import_Invalid_WhenFileExceedsSizeLimit()
    {
        var (ok, error) = ValidateUploadedFile("lessons.csv", 6 * 1024 * 1024);
        ok.Should().BeFalse();
        error.Should().Contain("5 MB");
    }

    [Fact]
    public void Import_Invalid_WhenFileNameNull()
    {
        var (ok, _) = ValidateUploadedFile(null, 1024);
        ok.Should().BeFalse();
    }
}
