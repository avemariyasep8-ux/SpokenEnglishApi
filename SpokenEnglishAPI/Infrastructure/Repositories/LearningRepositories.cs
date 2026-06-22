using Dapper;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Infrastructure.Repositories
{
    public class LessonRepository
    {
        private readonly DbContext _context;
        public LessonRepository(DbContext context) => _context = context;

        public async Task<IEnumerable<LessonDto>> GetLessons(int languageId)
        {
            using var con = _context.CreateConnection();
            // sp_lesson_getall includes is_premium flag
            return await con.QueryAsync<LessonDto>(
                "SELECT * FROM sp_lesson_getall(@p_languageid)",
                new { p_languageid = languageId });
        }

        public async Task<LessonDto?> GetLessonDetail(int lessonId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<LessonDto>(
                "SELECT * FROM sp_lesson_getdetail(@p_lessonid)",
                new { p_lessonid = lessonId });
        }

        public async Task<int> AddLesson(AddLessonDto dto)
        {
            using var con = _context.CreateConnection();
            return await con.ExecuteScalarAsync<int>(
                "SELECT sp_lesson_add(@p_lessontypeid, @p_lessonorder, @p_languageid, @p_lessonname, @p_description)",
                new { p_lessontypeid = dto.LessonTypeID, p_lessonorder = dto.LessonOrder, p_languageid = dto.LanguageID, p_lessonname = dto.LessonName, p_description = dto.Description });
        }

        public async Task UpdateLesson(UpdateLessonDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "SELECT sp_lesson_edit(@p_lessonid, @p_lessontypeid, @p_lessonorder, @p_isactive, @p_languageid, @p_lessonname, @p_description)",
                new { p_lessonid = dto.LessonID, p_lessontypeid = dto.LessonTypeID, p_lessonorder = dto.LessonOrder, p_isactive = dto.IsActive, p_languageid = dto.LanguageID, p_lessonname = dto.LessonName, p_description = dto.Description });
        }

        public async Task DeleteLesson(int lessonId)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync("SELECT sp_lesson_delete(@p_lessonid)", new { p_lessonid = lessonId });
        }
    }

    public class MeaningRepository
    {
        private readonly DbContext _context;
        public MeaningRepository(DbContext context) => _context = context;

        public async Task<IEnumerable<MeaningQuizDto>> GetMeaningQuestions(int lessonId, int languageId)
            => await GetMeaningQuestionsWithAnswers(lessonId, languageId);

        public async Task<IEnumerable<MeaningQuizDto>> GetMeaningQuestionsWithAnswers(int lessonId, int languageId)
        {
            using var con = _context.CreateConnection();
            var rows = await con.QueryAsync<dynamic>(
                "SELECT * FROM sp_meaningquestion_getwithansw(@p_lessonid, @p_languageid)",
                new { p_lessonid = lessonId, p_languageid = languageId });

            return rows.GroupBy(r => (int)r.questionid).Select(g => new MeaningQuizDto
            {
                QuestionID = g.Key,
                QuestionText = g.First().questiontext,
                Options = g.Select(r => new MeaningOptionDto
                {
                    OptionID = (int)r.optionid,
                    OptionText = (string)r.optiontext,
                    IsCorrect = r.iscorrect is bool b ? b : false
                }).ToList()
            });
        }

        public async Task<int> AddQuestion(AddMeaningQuestionDto dto)
        {
            using var con = _context.CreateConnection();
            return await con.ExecuteScalarAsync<int>(
                "SELECT sp_meaningquestion_add(@p_lessonid, @p_languageid, @p_questiontext)",
                new { p_lessonid = dto.LessonID, p_languageid = dto.LanguageID, p_questiontext = dto.QuestionText });
        }

        public async Task UpdateQuestion(UpdateMeaningQuestionDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "SELECT sp_meaningquestion_edit(@p_questionid, @p_languageid, @p_questiontext)",
                new { p_questionid = dto.QuestionID, p_languageid = dto.LanguageID, p_questiontext = dto.QuestionText });
        }

        public async Task DeleteQuestion(int questionId)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync("SELECT sp_meaningquestion_delete(@p_questionid)", new { p_questionid = questionId });
        }

        public async Task<int> AddOption(AddMeaningOptionDto dto)
        {
            using var con = _context.CreateConnection();
            return await con.ExecuteScalarAsync<int>(
                "SELECT sp_meaningoption_add(@p_questionid, @p_languageid, @p_optiontext, @p_iscorrect)",
                new { p_questionid = dto.QuestionID, p_languageid = dto.LanguageID, p_optiontext = dto.OptionText, p_iscorrect = dto.IsCorrect });
        }

        public async Task UpdateOption(UpdateMeaningOptionDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "SELECT sp_meaningoption_edit(@p_optionid, @p_languageid, @p_optiontext, @p_iscorrect)",
                new { p_optionid = dto.OptionID, p_languageid = dto.LanguageID, p_optiontext = dto.OptionText, p_iscorrect = dto.IsCorrect });
        }

        public async Task DeleteOption(int optionId)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync("SELECT sp_meaningoption_delete(@p_optionid)", new { p_optionid = optionId });
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
                "SELECT * FROM sp_arrangesentence_get(@p_lessonid, @p_languageid)",
                new { p_lessonid = lessonId, p_languageid = languageId });

            return rows.GroupBy(r => (int)r.arrangesentenceid).Select(g =>
            {
                var words = g.Select(r => new ArrangeWordDto
                {
                    WordID = (int)r.wordid,
                    WordText = (string)r.wordtext,
                    CorrectOrder = (int)r.correctorder
                }).OrderBy(_ => Guid.NewGuid()).ToList();

                return new ArrangeSentenceDto
                {
                    ArrangeSentenceID = g.Key,
                    CorrectSentence = g.First().correctsentence,
                    TamilMeaning = g.First().tamilmeaning as string,
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
                "SELECT * FROM sp_readingsentence_get(@p_lessonid, @p_languageid)",
                new { p_lessonid = lessonId, p_languageid = languageId });
        }
    }

    public class WordContentRepository
    {
        private readonly DbContext _context;
        public WordContentRepository(DbContext context) => _context = context;

        public async Task<IEnumerable<LessonWordContentDto>> GetWordContent(int lessonId, int languageId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryAsync<LessonWordContentDto>(
                "SELECT * FROM sp_lessonwordcontent_get(@p_lessonid, @p_languageid)",
                new { p_lessonid = lessonId, p_languageid = languageId });
        }
    }

    public class SubscriptionRepository
    {
        private readonly DbContext _context;
        public SubscriptionRepository(DbContext context) => _context = context;

        public virtual async Task<IEnumerable<SubscriptionPlanDto>> GetPlans()
        {
            using var con = _context.CreateConnection();
            return await con.QueryAsync<SubscriptionPlanDto>("SELECT * FROM sp_subscriptionplan_getall()");
        }

        public virtual async Task<int> Subscribe(SubscribeRequestDto dto)
        {
            using var con = _context.CreateConnection();
            return await con.ExecuteScalarAsync<int>(
                "SELECT sp_usersubscription_save(@p_userid, @p_planid, @p_paymentref)",
                new { p_userid = dto.UserId, p_planid = dto.PlanId, p_paymentref = dto.PaymentRef ?? "" });
        }

        public virtual async Task<UserSubscriptionDto?> GetUserSubscription(int userId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<UserSubscriptionDto>(
                "SELECT * FROM sp_usersubscription_get(@p_userid)",
                new { p_userid = userId });
        }
    }

    public class StreakRepository
    {
        private readonly DbContext _context;
        public StreakRepository(DbContext context) => _context = context;

        public async Task<UserStreakDto> UpdateStreak(int userId, int xpEarned)
        {
            using var con = _context.CreateConnection();
            return await con.QueryFirstAsync<UserStreakDto>(
                "SELECT * FROM sp_userstreak_update(@p_userid, @p_xp_earned)",
                new { p_userid = userId, p_xp_earned = xpEarned });
        }

        public async Task<UserStreakDto?> GetStreak(int userId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<UserStreakDto>(
                "SELECT * FROM sp_userstreak_get(@p_userid)",
                new { p_userid = userId });
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
                "SELECT sp_useranswer_save(@p_userid, @p_lessonid, @p_languageid, @p_activitytype, @p_referenceid, @p_iscorrect)",
                new { p_userid = dto.UserID, p_lessonid = dto.LessonID, p_languageid = dto.LanguageID, p_activitytype = dto.ActivityType, p_referenceid = dto.ReferenceID, p_iscorrect = dto.IsCorrect });
        }

        public async Task<IEnumerable<UserProgressDto>> GetUserProgress(int userId, int languageId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryAsync<UserProgressDto>(
                "SELECT * FROM sp_userprogress_get(@p_userid, @p_languageid)",
                new { p_userid = userId, p_languageid = languageId });
        }
    }
}
