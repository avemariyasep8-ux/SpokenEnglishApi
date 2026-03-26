using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressService _service;
        public ProgressController(IProgressService service) => _service = service;

        /// <summary>Save a user's answer for any activity.</summary>
        [HttpPost("answer")]
        public async Task<IActionResult> SaveAnswer([FromBody] SubmitAnswerDto dto)
        {
            await _service.SaveAnswer(dto);
            return Ok(new { Message = "Answer saved" });
        }

        /// <summary>Get user's progress summary grouped by lesson.</summary>
        [HttpGet("{userId}/{languageId}")]
        public async Task<IActionResult> GetProgress(int userId, int languageId)
        {
            var progress = await _service.GetUserProgress(userId, languageId);
            return Ok(progress);
        }
    }
}
