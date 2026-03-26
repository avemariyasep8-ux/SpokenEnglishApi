using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArrangeController : ControllerBase
    {
        private readonly IArrangeService _service;
        public ArrangeController(IArrangeService service) => _service = service;

        /// <summary>Get sentence-arrangement exercises for a lesson (words shuffled).</summary>
        [HttpGet("{lessonId}/{languageId}")]
        public async Task<IActionResult> GetSentences(int lessonId, int languageId)
        {
            var sentences = await _service.GetArrangeSentences(lessonId, languageId);
            return Ok(sentences);
        }
    }
}
