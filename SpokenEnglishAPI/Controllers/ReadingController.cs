using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReadingController : ControllerBase
    {
        private readonly IReadingService _service;
        public ReadingController(IReadingService service) => _service = service;

        /// <summary>Get reading-practice sentences for a lesson.</summary>
        [HttpGet("{lessonId}/{languageId}")]
        public async Task<IActionResult> GetSentences(int lessonId, int languageId)
        {
            var sentences = await _service.GetReadingSentences(lessonId, languageId);
            return Ok(sentences);
        }
    }
}
