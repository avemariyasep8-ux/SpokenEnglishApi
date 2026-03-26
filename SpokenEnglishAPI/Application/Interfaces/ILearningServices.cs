using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Application.Interfaces
{
    public interface ILessonService
    {
        Task<IEnumerable<LessonDto>> GetLessons(int languageId);
        Task<LessonDto?> GetLessonDetail(int lessonId);
        Task<int> AddLesson(AddLessonDto dto);
        Task UpdateLesson(UpdateLessonDto dto);
        Task DeleteLesson(int lessonId);
        Task<SequentialLessonDto?> GetSequentialLesson(int lessonId, int languageId);
    }

    public interface IMeaningService
    {
        Task<IEnumerable<MeaningQuizDto>> GetMeaningQuestions(int lessonId, int languageId);
        Task<IEnumerable<MeaningQuizDto>> GetMeaningQuestionsWithAnswers(int lessonId, int languageId);
        Task<int> AddQuestion(AddMeaningQuestionDto dto);
        Task UpdateQuestion(UpdateMeaningQuestionDto dto);
        Task DeleteQuestion(int questionId);
        Task<int> AddOption(AddMeaningOptionDto dto);
        Task UpdateOption(UpdateMeaningOptionDto dto);
        Task DeleteOption(int optionId);
    }

    public interface IArrangeService
    {
        Task<IEnumerable<ArrangeSentenceDto>> GetArrangeSentences(int lessonId, int languageId);
    }

    public interface IReadingService
    {
        Task<IEnumerable<ReadingSentenceDto>> GetReadingSentences(int lessonId, int languageId);
    }

    public interface IProgressService
    {
        Task SaveAnswer(SubmitAnswerDto dto);
        Task<IEnumerable<UserProgressDto>> GetUserProgress(int userId, int languageId);
    }
}
