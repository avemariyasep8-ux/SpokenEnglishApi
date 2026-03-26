using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Domain.DTOs;

namespace SpokenEnglishAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/speech")]
    public class SpeechController : ControllerBase
    {
        private readonly ISpeechService _speech;

        public SpeechController(ISpeechService speech)
        {
            _speech = speech;
        }

        [HttpPost("speech-to-text")]
        public async Task<IActionResult> Convert(SpeechRequestDto dto)
        {
            return Ok(await _speech.ConvertAsync(dto));
        }
    }

}
