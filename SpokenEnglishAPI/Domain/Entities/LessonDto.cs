namespace SpokenEnglishAPI.Domain.Entities
{
    public class LessonEntity
    {
        public int LessonID { get; set; }
        public string LessonName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TypeName { get; set; }
        public int LessonOrder { get; set; }
        public bool IsActive { get; set; }
        public int LessonTypeID { get; set; }
        public int LanguageID { get; set; }
    }

    public class MeaningQuestionEntity
    {
        public int QuestionID { get; set; }
        public int LessonID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<MeaningOptionEntity> Options { get; set; } = new();
    }

    public class MeaningOptionEntity
    {
        public int OptionID { get; set; }
        public int QuestionID { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class ArrangeSentenceEntity
    {
        public int ArrangeSentenceID { get; set; }
        public int LessonID { get; set; }
        public string CorrectSentence { get; set; } = string.Empty;
        public List<ArrangeWordEntity> Words { get; set; } = new();
    }

    public class ArrangeWordEntity
    {
        public int WordID { get; set; }
        public string WordText { get; set; } = string.Empty;
        public int CorrectOrder { get; set; }
    }

    public class ReadingSentenceEntity
    {
        public int ReadingSentenceID { get; set; }
        public int LessonID { get; set; }
        public string SentenceText { get; set; } = string.Empty;
        public string? ReferenceAudioUrl { get; set; }
    }

    public class UserProgressEntity
    {
        public int LessonID { get; set; }
        public int TotalAttempt { get; set; }
        public int CorrectCount { get; set; }
    }
}
