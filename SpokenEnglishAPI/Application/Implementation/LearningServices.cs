using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Repositories;

namespace SpokenEnglishAPI.Application.Implementation
{
    public class LessonService : ILessonService
    {
        private readonly LessonRepository _repo;
        private readonly MeaningRepository _meaningRepo;
        private readonly ArrangeRepository _arrangeRepo;
        private readonly ReadingRepository _readingRepo;

        public LessonService(
            LessonRepository repo,
            MeaningRepository meaningRepo,
            ArrangeRepository arrangeRepo,
            ReadingRepository readingRepo)
        {
            _repo = repo;
            _meaningRepo = meaningRepo;
            _arrangeRepo = arrangeRepo;
            _readingRepo = readingRepo;
        }

        public Task<IEnumerable<LessonDto>> GetLessons(int languageId) => _repo.GetLessons(languageId);
        public Task<LessonDto?> GetLessonDetail(int lessonId) => _repo.GetLessonDetail(lessonId);
        public Task<int> AddLesson(AddLessonDto dto) => _repo.AddLesson(dto);
        public Task UpdateLesson(UpdateLessonDto dto) => _repo.UpdateLesson(dto);
        public Task DeleteLesson(int lessonId) => _repo.DeleteLesson(lessonId);

        public async Task<SequentialLessonDto?> GetSequentialLesson(int lessonId, int languageId)
        {
            var lesson = await _repo.GetLessonDetail(lessonId);
            if (lesson == null) return null;

            var sequential = new SequentialLessonDto
            {
                LessonID = lesson.LessonID,
                LessonName = lesson.LessonName,
                Steps = new List<LessonStepDto>()
            };

            int order = 1;

            // 1. Meaning (Explanation + Definition)
            sequential.Steps.Add(new LessonStepDto
            {
                StepType = "meaning",
                Order = order++,
                Content = new StepContentDto { 
                    En = $"**Understanding the Lesson:**\n\n{lesson.Description ?? ""}\n\nDetailed explanation goes here.", 
                    Ta = "பாடம் விளக்கம் இங்கே இருக்கும்." 
                }
            });

            // 2. Examples
            var examples = await _readingRepo.GetReadingSentences(lessonId, languageId);
            sequential.Steps.Add(new LessonStepDto
            {
                StepType = "example",
                Order = order++,
                Content = examples.Select(e => new ExampleStepDto { En = e.SentenceText, Ta = "", AudioUrl = e.ReferenceAudioUrl })
            });

            // 3. Practice Arrange
            var arrange = await _arrangeRepo.GetArrangeSentences(lessonId, languageId);
            sequential.Steps.Add(new LessonStepDto
            {
                StepType = "practice_arrange",
                Order = order++,
                Content = arrange.Select(a => new PracticeArrangeDto { Words = a.Words.Select(w => w.WordText).ToList(), Correct = a.CorrectSentence })
            });

            // 4. Practice Speak (Reading)
            sequential.Steps.Add(new LessonStepDto
            {
                StepType = "practice_speak",
                Order = order++,
                Content = examples.Select(e => new PracticeSpeakDto { Text = e.SentenceText })
            });

            return sequential;
        }
    }

    public class MeaningService : IMeaningService
    {
        private readonly MeaningRepository _repo;
        public MeaningService(MeaningRepository repo) => _repo = repo;

        public Task<IEnumerable<MeaningQuizDto>> GetMeaningQuestions(int lessonId, int languageId)
            => _repo.GetMeaningQuestions(lessonId, languageId);

        public Task<IEnumerable<MeaningQuizDto>> GetMeaningQuestionsWithAnswers(int lessonId, int languageId)
            => _repo.GetMeaningQuestionsWithAnswers(lessonId, languageId);

        public Task<int> AddQuestion(AddMeaningQuestionDto dto) => _repo.AddQuestion(dto);
        public Task UpdateQuestion(UpdateMeaningQuestionDto dto) => _repo.UpdateQuestion(dto);
        public Task DeleteQuestion(int questionId) => _repo.DeleteQuestion(questionId);

        public Task<int> AddOption(AddMeaningOptionDto dto) => _repo.AddOption(dto);
        public Task UpdateOption(UpdateMeaningOptionDto dto) => _repo.UpdateOption(dto);
        public Task DeleteOption(int optionId) => _repo.DeleteOption(optionId);
    }

    public class ArrangeService : IArrangeService
    {
        private readonly ArrangeRepository _repo;
        public ArrangeService(ArrangeRepository repo) => _repo = repo;

        public Task<IEnumerable<ArrangeSentenceDto>> GetArrangeSentences(int lessonId, int languageId)
            => _repo.GetArrangeSentences(lessonId, languageId);
    }

    public class ReadingService : IReadingService
    {
        private readonly ReadingRepository _repo;
        public ReadingService(ReadingRepository repo) => _repo = repo;

        public Task<IEnumerable<ReadingSentenceDto>> GetReadingSentences(int lessonId, int languageId)
            => _repo.GetReadingSentences(lessonId, languageId);
    }

    public class ProgressService : IProgressService
    {
        private readonly ProgressRepository _repo;
        public ProgressService(ProgressRepository repo) => _repo = repo;

        public Task SaveAnswer(SubmitAnswerDto dto) => _repo.SaveAnswer(dto);
        public Task<IEnumerable<UserProgressDto>> GetUserProgress(int userId, int languageId)
            => _repo.GetUserProgress(userId, languageId);
    }
}
