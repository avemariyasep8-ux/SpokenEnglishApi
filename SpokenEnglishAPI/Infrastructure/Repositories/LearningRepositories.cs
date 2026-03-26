using Dapper;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Data;
using System.Data;

namespace SpokenEnglishAPI.Infrastructure.Repositories
{
    public class LessonRepository
    {
        private readonly DbContext _context;
        public LessonRepository(DbContext context) => _context = context;

        public async Task<IEnumerable<LessonDto>> GetLessons(int languageId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryAsync<LessonDto>(
                "sp_Lesson_Get",
                new { LanguageID = languageId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<LessonDto?> GetLessonDetail(int lessonId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<LessonDto>(
                "sp_Lesson",
                new { Mode = "GETDETAIL", LessonID = lessonId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> AddLesson(AddLessonDto dto)
        {
            using var con = _context.CreateConnection();
            var result = await con.QueryFirstOrDefaultAsync<dynamic>(
                "sp_Lesson",
                new
                {
                    Mode = "ADD",
                    LessonTypeID = dto.LessonTypeID,
                    LessonOrder = dto.LessonOrder,
                    LanguageID = dto.LanguageID,
                    LessonName = dto.LessonName,
                    Description = dto.Description
                },
                commandType: CommandType.StoredProcedure);
            return result?.LessonID ?? 0;
        }

        public async Task UpdateLesson(UpdateLessonDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "sp_Lesson",
                new
                {
                    Mode = "EDIT",
                    LessonID = dto.LessonID,
                    LessonTypeID = dto.LessonTypeID,
                    LessonOrder = dto.LessonOrder,
                    IsActive = dto.IsActive,
                    LanguageID = dto.LanguageID,
                    LessonName = dto.LessonName,
                    Description = dto.Description
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteLesson(int lessonId)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "sp_Lesson",
                new { Mode = "DELETE", LessonID = lessonId },
                commandType: CommandType.StoredProcedure);
        }
    }

    public class MeaningRepository
    {
        private readonly DbContext _context;
        public MeaningRepository(DbContext context) => _context = context;

        public async Task<IEnumerable<MeaningQuizDto>> GetMeaningQuestions(int lessonId, int languageId)
        {
            using var con = _context.CreateConnection();
            var rows = await con.QueryAsync<dynamic>(
                "sp_MeaningQuestion_Get",
                new { LessonID = lessonId, LanguageID = languageId },
                commandType: CommandType.StoredProcedure);

            var grouped = rows.GroupBy(r => (int)r.QuestionID);
            return grouped.Select(g => new MeaningQuizDto
            {
                QuestionID = g.Key,
                QuestionText = g.First().QuestionText,
                Options = g.Select(r => new MeaningOptionDto
                {
                    OptionID = (int)r.OptionID,
                    OptionText = (string)r.OptionText,
                    IsCorrect = false // don't expose correct answer to client
                }).ToList()
            });
        }

        public async Task<IEnumerable<MeaningQuizDto>> GetMeaningQuestionsWithAnswers(int lessonId, int languageId)
        {
            using var con = _context.CreateConnection();
            var rows = await con.QueryAsync<dynamic>(
                "sp_MeaningQuestion_Get",
                new { LessonID = lessonId, LanguageID = languageId },
                commandType: CommandType.StoredProcedure);

            var optionMap = new Dictionary<int, bool>();

            using var con2 = _context.CreateConnection();
            var correctOptions = await con2.QueryAsync<dynamic>(
                "SELECT OptionID, IsCorrect FROM MeaningOption WHERE QuestionID IN (SELECT QuestionID FROM MeaningQuestion WHERE LessonID = @LessonID)",
                new { LessonID = lessonId });
            foreach (var opt in correctOptions)
                optionMap[(int)opt.OptionID] = (bool)opt.IsCorrect;

            var grouped = rows.GroupBy(r => (int)r.QuestionID);
            return grouped.Select(g => new MeaningQuizDto
            {
                QuestionID = g.Key,
                QuestionText = g.First().QuestionText,
                Options = g.Select(r => new MeaningOptionDto
                {
                    OptionID = (int)r.OptionID,
                    OptionText = (string)r.OptionText,
                    IsCorrect = optionMap.ContainsKey((int)r.OptionID) && optionMap[(int)r.OptionID]
                }).ToList()
            });
        }

        public async Task<int> AddQuestion(AddMeaningQuestionDto dto)
        {
            using var con = _context.CreateConnection();
            var result = await con.QueryFirstOrDefaultAsync<dynamic>(
                "sp_MeaningQuestion",
                new
                {
                    Mode = "ADD",
                    LessonID = dto.LessonID,
                    LanguageID = dto.LanguageID,
                    QuestionText = dto.QuestionText
                },
                commandType: CommandType.StoredProcedure);
            return result?.QuestionID ?? 0;
        }

        public async Task UpdateQuestion(UpdateMeaningQuestionDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "sp_MeaningQuestion",
                new
                {
                    Mode = "EDIT",
                    QuestionID = dto.QuestionID,
                    LanguageID = dto.LanguageID,
                    QuestionText = dto.QuestionText
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteQuestion(int questionId)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "sp_MeaningQuestion",
                new { Mode = "DELETE", QuestionID = questionId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> AddOption(AddMeaningOptionDto dto)
        {
            using var con = _context.CreateConnection();
            var result = await con.QueryFirstOrDefaultAsync<dynamic>(
                "sp_MeaningOption",
                new
                {
                    Mode = "ADD",
                    QuestionID = dto.QuestionID,
                    LanguageID = dto.LanguageID,
                    OptionText = dto.OptionText,
                    IsCorrect = dto.IsCorrect
                },
                commandType: CommandType.StoredProcedure);
            return result?.OptionID ?? 0;
        }

        public async Task UpdateOption(UpdateMeaningOptionDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "sp_MeaningOption",
                new
                {
                    Mode = "EDIT",
                    OptionID = dto.OptionID,
                    LanguageID = dto.LanguageID,
                    OptionText = dto.OptionText,
                    IsCorrect = dto.IsCorrect
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteOption(int optionId)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "sp_MeaningOption",
                new { Mode = "DELETE", OptionID = optionId },
                commandType: CommandType.StoredProcedure);
        }
    }

    public class ArrangeRepository
    {
        private readonly DbContext _context;
        public ArrangeRepository(DbContext context) => _context = context;

        public async Task<IEnumerable<ArrangeSentenceDto>> GetArrangeSentences(int lessonId, int languageId)
        {
            using var con = _context.CreateConnection();
            var rows = await con.QueryAsync<dynamic>(
                "sp_ArrangeSentence_Get",
                new { LessonID = lessonId, LanguageID = languageId },
                commandType: CommandType.StoredProcedure);

            var grouped = rows.GroupBy(r => (int)r.ArrangeSentenceID);
            return grouped.Select(g =>
            {
                var words = g.Select(r => new ArrangeWordDto
                {
                    WordID = (int)r.WordID,
                    WordText = (string)r.WordText,
                    CorrectOrder = (int)r.CorrectOrder
                }).OrderBy(w => Guid.NewGuid()).ToList(); // shuffle words

                return new ArrangeSentenceDto
                {
                    ArrangeSentenceID = g.Key,
                    CorrectSentence = g.First().CorrectSentence,
                    Words = words
                };
            });
        }
    }

    public class ReadingRepository
    {
        private readonly DbContext _context;
        public ReadingRepository(DbContext context) => _context = context;

        public async Task<IEnumerable<ReadingSentenceDto>> GetReadingSentences(int lessonId, int languageId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryAsync<ReadingSentenceDto>(
                "sp_ReadingSentence_Get",
                new { LessonID = lessonId, LanguageID = languageId },
                commandType: CommandType.StoredProcedure);
        }
    }

    public class ProgressRepository
    {
        private readonly DbContext _context;
        public ProgressRepository(DbContext context) => _context = context;

        public async Task SaveAnswer(SubmitAnswerDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "sp_UserAnswer_Save",
                new
                {
                    UserID = dto.UserID,
                    LessonID = dto.LessonID,
                    LanguageID = dto.LanguageID,
                    ActivityType = dto.ActivityType,
                    ReferenceID = dto.ReferenceID,
                    IsCorrect = dto.IsCorrect
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<UserProgressDto>> GetUserProgress(int userId, int languageId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryAsync<UserProgressDto>(
                "sp_UserProgress_Get",
                new { UserID = userId, LanguageID = languageId },
                commandType: CommandType.StoredProcedure);
        }
    }
}
