using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Data;
using SpokenEnglishAPI.Infrastructure.Security;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]   // all progress data requires a valid token
    public class ProgressController : ControllerBase
    {
        private readonly IProgressService _service;
        private readonly DbContext _db;
        public ProgressController(IProgressService service, DbContext db) { _service = service; _db = db; }

        /// <summary>Save a user's answer for any activity.</summary>
        [HttpPost("answer")]
        public async Task<IActionResult> SaveAnswer([FromBody] SubmitAnswerDto dto)
        {
            if (!OwnershipGuard.CanAccess(User, dto.UserID)) return Forbid();
            await _service.SaveAnswer(dto);
            return Ok(new { Message = "Answer saved" });
        }

        /// <summary>Get user's progress summary grouped by lesson (answers only).</summary>
        [HttpGet("{userId}/{languageId}")]
        public async Task<IActionResult> GetProgress(int userId, int languageId)
        {
            if (!OwnershipGuard.CanAccess(User, userId)) return Forbid();
            var progress = await _service.GetUserProgress(userId, languageId);
            return Ok(progress);
        }

        /// <summary>Get all lessons with completion status, time spent and accuracy for a user.</summary>
        [HttpGet("lesson-summary/{userId}")]
        public async Task<IActionResult> GetLessonSummary(int userId)
        {
            if (!OwnershipGuard.CanAccess(User, userId)) return Forbid();
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT l.lessonid, ll.lessonname, l.lessonorder,
                         COALESCE(ulp.is_completed, false)       AS is_completed,
                         ulp.completed_date,
                         COALESCE(ulp.time_spent_seconds, 0)     AS time_spent_seconds,
                         COALESCE(ulp.correct_answers, 0)        AS correct_answers,
                         COALESCE(ulp.wrong_answers, 0)          AS wrong_answers,
                         COALESCE(ulp.total_attempts, 0)         AS total_attempts,
                         ulp.last_activity
                  FROM lesson l
                  JOIN lesson_lang ll ON ll.lessonid = l.lessonid AND ll.languageid = 1
                  LEFT JOIN user_lesson_progress ulp
                         ON ulp.lesson_id = l.lessonid AND ulp.user_id = @uid
                  WHERE l.isactive = true
                  ORDER BY l.lessonorder",
                new { uid = userId });
            return Ok(rows);
        }

        /// <summary>Mark a lesson as complete (or update time/score) for a user.</summary>
        [HttpPost("complete-lesson")]
        public async Task<IActionResult> CompleteLesson([FromBody] CompleteLessonDto dto)
        {
            if (!OwnershipGuard.CanAccess(User, dto.UserId)) return Forbid();
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"INSERT INTO user_lesson_progress
                    (user_id, lesson_id, is_completed, completed_date, time_spent_seconds,
                     correct_answers, wrong_answers, total_attempts, last_activity)
                  VALUES (@uid, @lid, true, NOW(), @time, @correct, @wrong, @attempts, NOW())
                  ON CONFLICT (user_id, lesson_id) DO UPDATE SET
                    is_completed      = true,
                    completed_date    = COALESCE(user_lesson_progress.completed_date, NOW()),
                    time_spent_seconds= user_lesson_progress.time_spent_seconds + EXCLUDED.time_spent_seconds,
                    correct_answers   = user_lesson_progress.correct_answers + EXCLUDED.correct_answers,
                    wrong_answers     = user_lesson_progress.wrong_answers + EXCLUDED.wrong_answers,
                    total_attempts    = user_lesson_progress.total_attempts + EXCLUDED.total_attempts,
                    last_activity     = NOW()",
                new { uid = dto.UserId, lid = dto.LessonId, time = dto.TimeSpentSeconds,
                      correct = dto.CorrectAnswers, wrong = dto.WrongAnswers,
                      attempts = dto.CorrectAnswers + dto.WrongAnswers });
            return Ok(new { message = "Lesson marked as complete" });
        }

        /// <summary>Reset a user's progress for a lesson (repractice from scratch).</summary>
        [HttpDelete("reset/{userId}/{lessonId}")]
        public async Task<IActionResult> ResetLesson(int userId, int lessonId)
        {
            if (!OwnershipGuard.CanAccess(User, userId)) return Forbid();
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                "DELETE FROM user_lesson_progress WHERE user_id=@uid AND lesson_id=@lid",
                new { uid = userId, lid = lessonId });
            await con.ExecuteAsync(
                "DELETE FROM useranswer WHERE userid=@uid AND lessonid=@lid",
                new { uid = userId, lid = lessonId });
            return Ok(new { message = "Progress reset" });
        }
    }

    public record CompleteLessonDto(int UserId, int LessonId, int TimeSpentSeconds, int CorrectAnswers, int WrongAnswers);
}
