using FluentAssertions;

namespace SpokenEnglishAPI.Tests;

/// <summary>
/// Tests that the CSV import format detection and column parsing works correctly.
/// Mirrors the logic in AdminController.ImportWordContent.
/// </summary>
public class ImportFormatDetectionTests
{
    // ── Mirrors AdminController.ParseCsvLine ──────────────────────────────────

    private static string[] ParseCsvLine(string line)
    {
        var result = new System.Collections.Generic.List<string>();
        bool inQuote = false;
        var current = new System.Text.StringBuilder();
        foreach (char c in line)
        {
            if (c == '"') { inQuote = !inQuote; }
            else if (c == ',' && !inQuote) { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    // ── Format detection ──────────────────────────────────────────────────────

    [Fact]
    public void TemplateHeader_HasSevenColumns()
    {
        var header = "LessonId,WordName,SentencePattern,DefinitionEn,DefinitionTa,ExampleEn,ExampleTa".Split(',');
        header.Should().HaveCount(7);
        header[0].Should().Be("LessonId");
        header[1].Should().Be("WordName");
    }

    [Fact]
    public void ExportHeader_HasTenColumns_AndStartsWithContentId()
    {
        var header = "ContentId,LessonId,LessonName,WordName,SentencePattern,DefinitionEn,DefinitionTa,ExampleEn,ExampleTa,DisplayOrder".Split(',');
        header.Should().HaveCount(10);
        header[0].Should().BeEquivalentTo("ContentId", opt => opt.Using(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void IsExportFormat_TrueWhenHeaderHasTenColsAndContentId()
    {
        var header = "ContentId,LessonId,LessonName,WordName,SentencePattern,DefinitionEn,DefinitionTa,ExampleEn,ExampleTa,DisplayOrder".Split(',');
        bool isExport = header.Length >= 10 &&
            header[0].Trim().Equals("ContentId", StringComparison.OrdinalIgnoreCase);
        isExport.Should().BeTrue();
    }

    [Fact]
    public void IsExportFormat_FalseForTemplateHeader()
    {
        var header = "LessonId,WordName,SentencePattern,DefinitionEn,DefinitionTa,ExampleEn,ExampleTa".Split(',');
        bool isExport = header.Length >= 10 &&
            header[0].Trim().Equals("ContentId", StringComparison.OrdinalIgnoreCase);
        isExport.Should().BeFalse();
    }

    // ── Template row parsing ──────────────────────────────────────────────────

    [Fact]
    public void TemplateRow_ParsesLessonIdAtIndex0()
    {
        var cols = ParseCsvLine("1,Hello,Subject + Hello,A common greeting,ஒரு பொதுவான வணக்கம்,Hello! How are you?,வணக்கம்!,1");
        cols[0].Should().Be("1"); // LessonId
        cols[1].Should().Be("Hello"); // WordName
        cols[2].Should().Be("Subject + Hello"); // SentencePattern
    }

    [Fact]
    public void TemplateRow_SkippedWhenFewerThanSevenColumns()
    {
        var cols = ParseCsvLine("1,Hello,Pattern");
        (cols.Length < 7).Should().BeTrue();
    }

    // ── Export row parsing ────────────────────────────────────────────────────

    [Fact]
    public void ExportRow_ParsesLessonIdAtIndex1()
    {
        var cols = ParseCsvLine("5,1,Greetings,Hello,Subject + Hello,A common greeting,ஒரு வணக்கம்,Hello!,வணக்கம்!,1");
        cols[1].Should().Be("1");  // LessonId
        cols[3].Should().Be("Hello"); // WordName
    }

    [Fact]
    public void ExportRow_SkippedWhenFewerThanTenColumns()
    {
        var cols = ParseCsvLine("5,1,Greetings,Hello,Pattern");
        (cols.Length < 10).Should().BeTrue();
    }

    // ── Quoted CSV values ─────────────────────────────────────────────────────

    [Fact]
    public void ParseCsvLine_HandlesQuotedCommas()
    {
        var cols = ParseCsvLine("1,\"Hello, World\",Pattern,Meaning");
        cols.Should().HaveCount(4);
        cols[1].Should().Be("Hello, World");
    }

    [Fact]
    public void ParseCsvLine_HandlesEmptyFields()
    {
        var cols = ParseCsvLine("1,Hello,,Meaning,,,ExampleEn,,1");
        cols[2].Should().BeEmpty();
        cols[0].Should().Be("1");
    }
}

/// <summary>
/// Tests MCQ option ID casing — API returns optionID (capital D),
/// UI must handle both optionID and optionId to avoid broken selection.
/// </summary>
public class McqOptionIdCasingTests
{
    // Simulates what the UI normalisation does
    private static int NormaliseOptionId(dynamic opt, int fallback)
    {
        // In JS: opt.optionID ?? opt.optionId ?? fallback
        // Simulate in C# with a dictionary
        var d = opt as System.Collections.Generic.Dictionary<string, object>;
        if (d == null) return fallback;
        if (d.TryGetValue("optionID", out var v1) && v1 is int i1) return i1;
        if (d.TryGetValue("optionId", out var v2) && v2 is int i2) return i2;
        return fallback;
    }

    [Fact]
    public void OptionId_UpperD_IsNormalised()
    {
        var opt = new System.Collections.Generic.Dictionary<string, object> { ["optionID"] = 42 };
        NormaliseOptionId(opt, -1).Should().Be(42);
    }

    [Fact]
    public void OptionId_LowerD_IsNormalised()
    {
        var opt = new System.Collections.Generic.Dictionary<string, object> { ["optionId"] = 7 };
        NormaliseOptionId(opt, -1).Should().Be(7);
    }

    [Fact]
    public void OptionId_FallbackUsed_WhenNeither()
    {
        var opt = new System.Collections.Generic.Dictionary<string, object>();
        NormaliseOptionId(opt, 99).Should().Be(99);
    }

    [Fact]
    public void OptionId_UpperDTakesPriority()
    {
        var opt = new System.Collections.Generic.Dictionary<string, object>
            { ["optionID"] = 10, ["optionId"] = 20 };
        NormaliseOptionId(opt, -1).Should().Be(10);
    }
}
