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
                @"SELECT l.lessonid, lt.typename AS LessonType, ll.lessonname, ll.description, l.lessonorder
                  FROM lesson l
                  JOIN lessontype lt ON l.lessontypeid = lt.lessontypeid
                  JOIN lesson_lang ll ON l.lessonid = ll.lessonid
                  WHERE ll.languageid = @LanguageID AND l.isactive = true
                  ORDER BY l.lessonorder",
                new { LanguageID = languageId });
        }

        public async Task<LessonDto?> GetLessonDetail(int lessonId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryFirstOrDefaultAsync<LessonDto>(
                @"SELECT l.lessonid, l.lessontypeid, l.lessonorder, l.isactive,
                         ll.languageid, ll.lessonname, ll.description
                  FROM lesson l
                  JOIN lesson_lang ll ON l.lessonid = ll.lessonid
                  WHERE l.lessonid = @LessonID",
                new { LessonID = lessonId });
        }

        public async Task<int> AddLesson(AddLessonDto dto)
        {
            using var con = _context.CreateConnection();
            var lessonId = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO lesson (lessontypeid, lessonorder, isactive)
                  VALUES (@LessonTypeID, @LessonOrder, true)
                  RETURNING lessonid",
                new { dto.LessonTypeID, dto.LessonOrder });

            await con.ExecuteAsync(
                @"INSERT INTO lesson_lang (lessonid, languageid, lessonname, description)
                  VALUES (@LessonID, @LanguageID, @LessonName, @Description)",
                new { LessonID = lessonId, dto.LanguageID, dto.LessonName, dto.Description });

            return lessonId;
        }

        public async Task UpdateLesson(UpdateLessonDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE lesson SET lessontypeid = @LessonTypeID, lessonorder = @LessonOrder, isactive = @IsActive
                  WHERE lessonid = @LessonID",
                new { dto.LessonTypeID, dto.LessonOrder, dto.IsActive, dto.LessonID });

            await con.ExecuteAsync(
                @"UPDATE lesson_lang SET lessonname = @LessonName, description = @Description
                  WHERE lessonid = @LessonID AND languageid = @LanguageID",
                new { dto.LessonName, dto.Description, dto.LessonID, dto.LanguageID });
        }

        public async Task DeleteLesson(int lessonId)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync("DELETE FROM lesson_lang WHERE lessonid = @LessonID", new { LessonID = lessonId });
            await con.ExecuteAsync("DELETE FROM lesson WHERE lessonid = @LessonID", new { LessonID = lessonId });
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
                @"SELECT mq.questionid, mql.questiontext, mo.optionid, mol.optiontext
                  FROM meaningquestion mq
                  JOIN meaningquestion_lang mql ON mq.questionid = mql.questionid AND mql.languageid = @LanguageID
                  JOIN meaningoption mo ON mq.questionid = mo.questionid
                  JOIN meaningoption_lang mol ON mo.optionid = mol.optionid AND mol.languageid = @LanguageID
                  WHERE mq.lessonid = @LessonID
                  ORDER BY mq.questionid, mo.optionid",
                new { LessonID = lessonId, LanguageID = languageId });

            return rows.GroupBy(r => (int)r.questionid).Select(g => new MeaningQuizDto
            {
                QuestionID = g.Key,
                QuestionText = g.First().questiontext,
                Options = g.Select(r => new MeaningOptionDto
                {
                    OptionID = (int)r.optionid,
                    OptionText = (string)r.optiontext,
                    IsCorrect = false
                }).ToList()
            });
        }

        public async Task<IEnumerable<MeaningQuizDto>> GetMeaningQuestionsWithAnswers(int lessonId, int languageId)
        {
            using var con = _context.CreateConnection();
            var rows = await con.QueryAsync<dynamic>(
                @"SELECT mq.questionid, mql.questiontext, mo.optionid, mol.optiontext, mo.iscorrect
                  FROM meaningquestion mq
                  JOIN meaningquestion_lang mql ON mq.questionid = mql.questionid AND mql.languageid = @LanguageID
                  JOIN meaningoption mo ON mq.questionid = mo.questionid
                  JOIN meaningoption_lang mol ON mo.optionid = mol.optionid AND mol.languageid = @LanguageID
                  WHERE mq.lessonid = @LessonID
                  ORDER BY mq.questionid, mo.optionid",
                new { LessonID = lessonId, LanguageID = languageId });

            return rows.GroupBy(r => (int)r.questionid).Select(g => new MeaningQuizDto
            {
                QuestionID = g.Key,
                QuestionText = g.First().questiontext,
                Options = g.Select(r => new MeaningOptionDto
                {
                    OptionID = (int)r.optionid,
                    OptionText = (string)r.optiontext,
                    IsCorrect = (bool)r.iscorrect
                }).ToList()
            });
        }

        public async Task<int> AddQuestion(AddMeaningQuestionDto dto)
        {
            using var con = _context.CreateConnection();
            var questionId = await con.ExecuteScalarAsync<int>(
                "INSERT INTO meaningquestion (lessonid) VALUES (@LessonID) RETURNING questionid",
                new { dto.LessonID });

            await con.ExecuteAsync(
                "INSERT INTO meaningquestion_lang (questionid, languageid, questiontext) VALUES (@QuestionID, @LanguageID, @QuestionText)",
                new { QuestionID = questionId, dto.LanguageID, dto.QuestionText });

            return questionId;
        }

        public async Task UpdateQuestion(UpdateMeaningQuestionDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "UPDATE meaningquestion_lang SET questiontext = @QuestionText WHERE questionid = @QuestionID AND languageid = @LanguageID",
                new { dto.QuestionText, dto.QuestionID, dto.LanguageID });
        }

        public async Task DeleteQuestion(int questionId)
        {
            using var con = _context.CreateConnection();
            var optionIds = (await con.QueryAsync<int>(
                "SELECT optionid FROM meaningoption WHERE questionid = @QuestionID", new { QuestionID = questionId })).ToList();

            if (optionIds.Any())
                await con.ExecuteAsync("DELETE FROM meaningoption_lang WHERE optionid = ANY(@Ids)", new { Ids = optionIds });

            await con.ExecuteAsync("DELETE FROM meaningoption WHERE questionid = @QuestionID", new { QuestionID = questionId });
            await con.ExecuteAsync("DELETE FROM meaningquestion_lang WHERE questionid = @QuestionID", new { QuestionID = questionId });
            await con.ExecuteAsync("DELETE FROM meaningquestion WHERE questionid = @QuestionID", new { QuestionID = questionId });
        }

        public async Task<int> AddOption(AddMeaningOptionDto dto)
        {
            using var con = _context.CreateConnection();
            var optionId = await con.ExecuteScalarAsync<int>(
                "INSERT INTO meaningoption (questionid, iscorrect) VALUES (@QuestionID, @IsCorrect) RETURNING optionid",
                new { dto.QuestionID, dto.IsCorrect });

            await con.ExecuteAsync(
                "INSERT INTO meaningoption_lang (optionid, languageid, optiontext) VALUES (@OptionID, @LanguageID, @OptionText)",
                new { OptionID = optionId, dto.LanguageID, dto.OptionText });

            return optionId;
        }

        public async Task UpdateOption(UpdateMeaningOptionDto dto)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync(
                "UPDATE meaningoption SET iscorrect = @IsCorrect WHERE optionid = @OptionID",
                new { dto.IsCorrect, dto.OptionID });

            await con.ExecuteAsync(
                "UPDATE meaningoption_lang SET optiontext = @OptionText WHERE optionid = @OptionID AND languageid = @LanguageID",
                new { dto.OptionText, dto.OptionID, dto.LanguageID });
        }

        public async Task DeleteOption(int optionId)
        {
            using var con = _context.CreateConnection();
            await con.ExecuteAsync("DELETE FROM meaningoption_lang WHERE optionid = @OptionID", new { OptionID = optionId });
            await con.ExecuteAsync("DELETE FROM meaningoption WHERE optionid = @OptionID", new { OptionID = optionId });
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
                @"SELECT asl.arrangesentenceid, asl.correctsentence, asw.wordid, aswl.wordtext, asw.correctorder
                  FROM arrangesentence asn
                  JOIN arrangesentence_lang asl ON asn.arrangesentenceid = asl.arrangesentenceid AND asl.languageid = @LanguageID
                  JOIN arrangesentenceword asw ON asn.arrangesentenceid = asw.arrangesentenceid
                  JOIN arrangesentenceword_lang aswl ON asw.wordid = aswl.wordid AND aswl.languageid = @LanguageID
                  WHERE asn.lessonid = @LessonID
                  ORDER BY asw.correctorder",
                new { LessonID = lessonId, LanguageID = languageId });

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
                @"SELECT rs.readingsentenceid, rsl.sentencetext, rsl.referenceaudiourl
                  FROM readingsentence rs
                  JOIN readingsentence_lang rsl ON rs.readingsentenceid = rsl.readingsentenceid
                  WHERE rs.lessonid = @LessonID AND rsl.languageid = @LanguageID",
                new { LessonID = lessonId, LanguageID = languageId });
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
                @"INSERT INTO useranswer (userid, lessonid, languageid, activitytype, referenceid, iscorrect)
                  VALUES (@UserID, @LessonID, @LanguageID, @ActivityType, @ReferenceID, @IsCorrect)",
                new { dto.UserID, dto.LessonID, dto.LanguageID, dto.ActivityType, dto.ReferenceID, dto.IsCorrect });
        }

        public async Task<IEnumerable<UserProgressDto>> GetUserProgress(int userId, int languageId)
        {
            using var con = _context.CreateConnection();
            return await con.QueryAsync<UserProgressDto>(
                @"SELECT lessonid, COUNT(*) AS TotalAttempt,
                         SUM(CASE WHEN iscorrect = true THEN 1 ELSE 0 END) AS CorrectCount
                  FROM useranswer
                  WHERE userid = @UserID AND languageid = @LanguageID
                  GROUP BY lessonid",
                new { UserID = userId, LanguageID = languageId });
        }
    }
}
