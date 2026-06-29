namespace SpokenEnglishAPI.Domain.DTOs
{
    // ─── WORD CONTENT (definition, pattern, example cards) ──────
    public class LessonWordContentDto
    {
        public int ContentId { get; set; }
        public int LessonId { get; set; }
        public string WordName { get; set; } = string.Empty;
        public string? SentencePattern { get; set; }
        public string DefinitionEn { get; set; } = string.Empty;
        public string? DefinitionTa { get; set; }
        public string? ExampleEn { get; set; }
        public string? ExampleTa { get; set; }
        public int DisplayOrder { get; set; }
    }

    // ─── SUBSCRIPTION ────────────────────────────────────────────
    public class SubscriptionPlanDto
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public int DurationMonths { get; set; }
        public decimal PriceInr { get; set; }
        public string? Features { get; set; }
    }

    public class UserSubscriptionDto
    {
        public int SubscriptionId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int DaysRemaining { get; set; }
    }

    public class SubscribeRequestDto
    {
        public int UserId { get; set; }
        public int PlanId { get; set; }
        public string? PaymentRef { get; set; }
    }

    // ─── STREAK ──────────────────────────────────────────────────
    public class UserStreakDto
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalXp { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }

    public class UpdateStreakDto
    {
        public int UserId { get; set; }
        public int XpEarned { get; set; }
    }


    // ─── LESSON ────────────────────────────────────────────────
    public class LessonDto
    {
        public int LessonID { get; set; }
        public string LessonName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TypeName { get; set; }
        public int LessonOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsPremium { get; set; }
        public string Level { get; set; } = "Beginner";
    }

    public class AddLessonDto
    {
        public int LessonTypeID { get; set; }
        public int LessonOrder { get; set; }
        public int LanguageID { get; set; }
        public string LessonName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateLessonDto
    {
        public int LessonID { get; set; }
        public int LessonTypeID { get; set; }
        public int LessonOrder { get; set; }
        public bool IsActive { get; set; }
        public int LanguageID { get; set; }
        public string LessonName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // ─── MEANING QUIZ ────────────────────────────────────────────
    public class MeaningQuizDto
    {
        public int QuestionID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<MeaningOptionDto> Options { get; set; } = new();
    }

    public class MeaningOptionDto
    {
        public int OptionID { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class AddMeaningQuestionDto
    {
        public int LessonID { get; set; }
        public int LanguageID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
    }

    public class UpdateMeaningQuestionDto
    {
        public int QuestionID { get; set; }
        public int LanguageID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
    }

    public class AddMeaningOptionDto
    {
        public int QuestionID { get; set; }
        public int LanguageID { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class UpdateMeaningOptionDto
    {
        public int OptionID { get; set; }
        public int LanguageID { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    // ─── ARRANGE SENTENCE ────────────────────────────────────────
    public class ArrangeSentenceDto
    {
        public int ArrangeSentenceID { get; set; }
        public string CorrectSentence { get; set; } = string.Empty;
        public string? TamilMeaning { get; set; }
        public List<ArrangeWordDto> Words { get; set; } = new();
    }

    public class ArrangeWordDto
    {
        public int WordID { get; set; }
        public string WordText { get; set; } = string.Empty;
        public int CorrectOrder { get; set; }
    }

    // ─── READING SENTENCE ────────────────────────────────────────
    public class ReadingSentenceDto
    {
        public int ReadingSentenceID { get; set; }
        public string SentenceText { get; set; } = string.Empty;
        public string? ReferenceAudioUrl { get; set; }
    }

    // ─── USER ANSWER & PROGRESS ──────────────────────────────────
    public class SubmitAnswerDto
    {
        public int UserID { get; set; }
        public int LessonID { get; set; }
        public int LanguageID { get; set; }
        public string ActivityType { get; set; } = string.Empty; // "Meaning" | "Arrange" | "Reading"
        public int ReferenceID { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class UserProgressDto
    {
        public int LessonID { get; set; }
        public long TotalAttempt { get; set; }
        public long CorrectCount { get; set; }
        public int Accuracy => TotalAttempt == 0 ? 0 : (int)((double)CorrectCount / TotalAttempt * 100);
    }

    // ─── SEQUENTIAL LESSON FLOW ──────────────────────────────────
    public class SequentialLessonDto
    {
        public int LessonID { get; set; }
        public string LessonName { get; set; } = string.Empty;
        public List<LessonStepDto> Steps { get; set; } = new();
    }

    public class LessonStepDto
    {
        public string StepType { get; set; } = string.Empty; // "definition" | "structure" | "example" | "meaning" | "practice_arrange" | "practice_speak"
        public int Order { get; set; }
        public object? Content { get; set; } // Can be DefinitionContent, StructureContent, etc.
    }

    public class StepContentDto
    {
        public string En { get; set; } = string.Empty;
        public string Ta { get; set; } = string.Empty;
    }

    public class ExampleStepDto
    {
        public string En { get; set; } = string.Empty;
        public string Ta { get; set; } = string.Empty;
        public string? AudioUrl { get; set; }
    }

    public class PracticeArrangeDto
    {
        public List<string> Words { get; set; } = new();
        public string Correct { get; set; } = string.Empty;
    }

    public class PracticeSpeakDto
    {
        public string Text { get; set; } = string.Empty;
    }
}
