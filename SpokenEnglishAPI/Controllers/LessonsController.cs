using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _service;
        public LessonsController(ILessonService service) => _service = service;

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
