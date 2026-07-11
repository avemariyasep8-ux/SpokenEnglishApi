using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Application.Interfaces;

namespace SpokenEnglishAPI.Controllers
{
    /// <summary>
    /// Free-form AI conversation practice — unlike the scripted Conversation feature,
    /// the learner can say anything and the AI replies naturally + corrects mistakes.
    /// Stateless: the client keeps the running history and resends it each turn.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AiConversationController : ControllerBase
    {
        private readonly IAiConversationService _ai;
        public AiConversationController(IAiConversationService ai) => _ai = ai;

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest(new { message = "Message is required" });
            if (dto.Message.Length > 500)
                return BadRequest(new { message = "Message is too long (max 500 characters)" });

            var history = (dto.History ?? new())
                .TakeLast(12) // cap context sent to the model
                .Select(h => new AiChatTurn(h.Role, h.Text))
                .ToList();

            try
            {
                var result = await _ai.ChatAsync(
                    string.IsNullOrWhiteSpace(dto.Scenario) ? "General everyday conversation" : dto.Scenario,
                    string.IsNullOrWhiteSpace(dto.Level) ? "Beginner" : dto.Level,
                    history, dto.Message);

                return Ok(new
                {
                    reply = result.Reply,
                    hasMistake = result.HasMistake,
                    correctedText = result.CorrectedText,
                    explanation = result.Explanation
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("GEMINI_API_KEY"))
            {
                return StatusCode(503, new { message = "AI conversation is not configured yet. Please contact the admin." });
            }
            catch (Exception)
            {
                return StatusCode(502, new { message = "AI conversation partner is unavailable right now. Please try again in a moment." });
            }
        }
    }

    public record AiChatTurnDto(string Role, string Text);
    public record AiChatRequestDto(string Message, string? Scenario, string? Level, List<AiChatTurnDto>? History);
}
