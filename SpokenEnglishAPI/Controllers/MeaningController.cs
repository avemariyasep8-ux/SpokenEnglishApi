using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeaningController : ControllerBase
    {
        private readonly IMeaningService _service;
        public MeaningController(IMeaningService service) => _service = service;

        /// <summary>Get word meaning MCQ questions for a lesson.</summary>
        [HttpGet("{lessonId}/{languageId}")]
        public async Task<IActionResult> GetQuestions(int lessonId, int languageId)
        {
            var questions = await _service.GetMeaningQuestions(lessonId, languageId);
            return Ok(questions);
        }

        /// <summary>Get questions with correct answers (for admin/editor).</summary>
        [HttpGet("admin/{lessonId}/{languageId}")]
        [Authorize]
        public async Task<IActionResult> GetQuestionsAdmin(int lessonId, int languageId)
        {
            var questions = await _service.GetMeaningQuestionsWithAnswers(lessonId, languageId);
            return Ok(questions);
        }

        [HttpPost("question")]
        [Authorize]
        public async Task<IActionResult> AddQuestion([FromBody] AddMeaningQuestionDto dto)
        {
            var id = await _service.AddQuestion(dto);
            return Ok(new { QuestionID = id });
        }

        [HttpPut("question")]
        [Authorize]
        public async Task<IActionResult> UpdateQuestion([FromBody] UpdateMeaningQuestionDto dto)
        {
            await _service.UpdateQuestion(dto);
            return Ok(new { Message = "Question updated" });
        }

        [HttpDelete("question/{questionId}")]
        [Authorize]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            await _service.DeleteQuestion(questionId);
            return Ok(new { Message = "Question deleted" });
        }

        [HttpPost("option")]
        [Authorize]
        public async Task<IActionResult> AddOption([FromBody] AddMeaningOptionDto dto)
        {
            var id = await _service.AddOption(dto);
            return Ok(new { OptionID = id });
        }

        [HttpPut("option")]
        [Authorize]
        public async Task<IActionResult> UpdateOption([FromBody] UpdateMeaningOptionDto dto)
        {
            await _service.UpdateOption(dto);
            return Ok(new { Message = "Option updated" });
        }

        [HttpDelete("option/{optionId}")]
        [Authorize]
        public async Task<IActionResult> DeleteOption(int optionId)
        {
            await _service.DeleteOption(optionId);
            return Ok(new { Message = "Option deleted" });
        }
    }
}
