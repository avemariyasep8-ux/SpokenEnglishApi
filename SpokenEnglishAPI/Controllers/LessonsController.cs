using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _service;
        private readonly DbContext _db;
        public LessonsController(ILessonService service, DbContext db)
        {
            _service = service;
            _db = db;
        }

        /// <summary>Get all lessons for a language.</summary>
        [HttpGet("{languageId}")]
        public async Task<IActionResult> GetLessons(int languageId)
        {
            var lessons = await _service.GetLessons(languageId);
            return Ok(lessons);
        }

        /// <summary>Get lesson detail by ID.</summary>
        [HttpGet("detail/{lessonId}")]
        public async Task<IActionResult> GetLessonDetail(int lessonId)
        {
            var lesson = await _service.GetLessonDetail(lessonId);
            return lesson == null ? NotFound() : Ok(lesson);
        }

        /// <summary>Get sequential lesson flow.</summary>
        [HttpGet("flow/{lessonId}/{languageId}")]
        public async Task<IActionResult> GetSequentialLesson(int lessonId, int languageId)
        {
            var flow = await _service.GetSequentialLesson(lessonId, languageId);
            return flow == null ? NotFound() : Ok(flow);
        }

        /// <summary>Get full lesson content (words, MCQ, fill-in, arrange) — accessible to all users.</summary>
        [HttpGet("content/{lessonId}")]
        public async Task<IActionResult> GetLessonContent(int lessonId)
        {
            using var con = _db.CreateConnection();

            var wordContent = await con.QueryAsync(
                @"SELECT content_id as id, word_name, sentence_pattern, definition_en, definition_ta,
                         example_en, example_ta, display_order
                  FROM lesson_word_content WHERE lesson_id=@lid ORDER BY display_order",
                new { lid = lessonId });

            var mcq = await con.QueryAsync(
                @"SELECT q.questionid as id, mql.questiontext,
                         json_agg(json_build_object('optionid', o.optionid, 'optiontext', ol.optiontext, 'iscorrect', o.iscorrect)
                                  ORDER BY o.optionid) as options
                  FROM meaningquestion q
                  JOIN meaningquestion_lang mql ON mql.questionid=q.questionid AND mql.languageid=1
                  JOIN meaningoption o ON o.questionid=q.questionid
                  JOIN meaningoption_lang ol ON ol.optionid=o.optionid AND ol.languageid=1
                  WHERE q.lessonid=@lid
                  GROUP BY q.questionid, mql.questiontext ORDER BY q.questionid",
                new { lid = lessonId });

            var fillin = await con.QueryAsync(
                @"SELECT id, sentence_with_blank, correct_answer, option1, option2, option3, hint_ta, display_order
                  FROM fillinblank WHERE lessonid=@lid ORDER BY display_order",
                new { lid = lessonId });

            var arrange = await con.QueryAsync(
                @"SELECT a.arrangesentenceid as id, al.correctsentence
                  FROM arrangesentence a
                  JOIN arrangesentence_lang al ON al.arrangesentenceid=a.arrangesentenceid AND al.languageid=1
                  WHERE a.lessonid=@lid ORDER BY a.arrangesentenceid",
                new { lid = lessonId });

            return Ok(new { wordContent, mcq, fillin, arrange });
        }

        /// <summary>Add a new lesson.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddLesson([FromBody] AddLessonDto dto)
        {
            var id = await _service.AddLesson(dto);
            return Ok(new { LessonID = id });
        }

        /// <summary>Update an existing lesson.</summary>
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateLesson([FromBody] UpdateLessonDto dto)
        {
            await _service.UpdateLesson(dto);
            return Ok(new { Message = "Lesson updated successfully" });
        }

        /// <summary>Delete a lesson.</summary>
        [HttpDelete("{lessonId}")]
        [Authorize]
        public async Task<IActionResult> DeleteLesson(int lessonId)
        {
            await _service.DeleteLesson(lessonId);
            return Ok(new { Message = "Lesson deleted successfully" });
        }
    }
}
