using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Controllers
{
    /// <summary>
    /// Conversation lessons — interactive dialogue scenarios (Tea Shop, Restaurant, ...).
    /// Each conversation has ordered turns; a turn with an expected_response requires the
    /// learner to speak/type that reply before advancing. Fully additive: does not touch
    /// the existing lesson/play flow.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationController : ControllerBase
    {
        private readonly DbContext _db;
        public ConversationController(DbContext db) => _db = db;

        // ── GET /api/conversation ─────────────────────────────────────────
        // Public list of active conversations.
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? level = null)
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT c.conversation_id, c.title, c.scenario, c.description, c.level, c.display_order,
                         (SELECT COUNT(*) FROM conversation_turn t WHERE t.conversation_id = c.conversation_id) AS turn_count
                  FROM conversation c
                  WHERE c.is_active = true AND (@level IS NULL OR c.level = @level)
                  ORDER BY c.display_order, c.conversation_id",
                new { level });
            return Ok(rows);
        }

        // ── GET /api/conversation/admin ───────────────────────────────────
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAdmin()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT c.conversation_id, c.title, c.scenario, c.description, c.level, c.display_order, c.is_active,
                         (SELECT COUNT(*) FROM conversation_turn t WHERE t.conversation_id = c.conversation_id) AS turn_count
                  FROM conversation c
                  ORDER BY c.display_order, c.conversation_id");
            return Ok(rows);
        }

        // ── GET /api/conversation/{id} ────────────────────────────────────
        // Detail with ordered turns (used by the play flow and admin editor).
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            using var con = _db.CreateConnection();
            var conv = await con.QueryFirstOrDefaultAsync(
                "SELECT conversation_id, title, scenario, description, level, display_order, is_active FROM conversation WHERE conversation_id=@id",
                new { id });
            if (conv == null) return NotFound(new { message = "Conversation not found" });

            var turns = await con.QueryAsync(
                @"SELECT turn_id, turn_order, system_text, expected_response, tamil_hint
                  FROM conversation_turn WHERE conversation_id=@id ORDER BY turn_order, turn_id",
                new { id });

            return Ok(new { conversation = conv, turns });
        }

        // ── POST /api/conversation (Admin) ────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] ConversationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest(new { message = "Title required" });
            using var con = _db.CreateConnection();
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO conversation (title, scenario, description, level, display_order, is_active)
                  VALUES (@title, @scenario, @desc, @level, @ord, true) RETURNING conversation_id",
                new { title = dto.Title, scenario = dto.Scenario, desc = dto.Description,
                      level = string.IsNullOrWhiteSpace(dto.Level) ? "Beginner" : dto.Level, ord = dto.DisplayOrder });
            return Ok(new { id, message = "Conversation created" });
        }

        // ── PUT /api/conversation/{id} (Admin) ────────────────────────────
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ConversationDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE conversation SET title=@title, scenario=@scenario, description=@desc,
                  level=@level, display_order=@ord, is_active=@active WHERE conversation_id=@id",
                new { title = dto.Title, scenario = dto.Scenario, desc = dto.Description,
                      level = string.IsNullOrWhiteSpace(dto.Level) ? "Beginner" : dto.Level,
                      ord = dto.DisplayOrder, active = dto.IsActive, id });
            return Ok(new { message = "Conversation updated" });
        }

        // ── DELETE /api/conversation/{id} (Admin) ─────────────────────────
        // Hard delete — turns cascade via FK.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("DELETE FROM conversation WHERE conversation_id=@id", new { id });
            return Ok(new { message = "Conversation deleted" });
        }

        // ── POST /api/conversation/{id}/turn (Admin) ──────────────────────
        [HttpPost("{id}/turn")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddTurn(int id, [FromBody] TurnDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SystemText)) return BadRequest(new { message = "System text required" });
            using var con = _db.CreateConnection();
            var order = dto.TurnOrder > 0 ? dto.TurnOrder
                : await con.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(turn_order),0)+1 FROM conversation_turn WHERE conversation_id=@id", new { id });
            var turnId = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO conversation_turn (conversation_id, turn_order, system_text, expected_response, tamil_hint)
                  VALUES (@id, @ord, @sys, @exp, @ta) RETURNING turn_id",
                new { id, ord = order, sys = dto.SystemText,
                      exp = string.IsNullOrWhiteSpace(dto.ExpectedResponse) ? null : dto.ExpectedResponse,
                      ta = string.IsNullOrWhiteSpace(dto.TamilHint) ? null : dto.TamilHint });
            return Ok(new { turnId, message = "Turn added" });
        }

        // ── PUT /api/conversation/turn/{turnId} (Admin) ───────────────────
        [HttpPut("turn/{turnId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTurn(int turnId, [FromBody] TurnDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE conversation_turn SET turn_order=@ord, system_text=@sys,
                  expected_response=@exp, tamil_hint=@ta WHERE turn_id=@id",
                new { ord = dto.TurnOrder, sys = dto.SystemText,
                      exp = string.IsNullOrWhiteSpace(dto.ExpectedResponse) ? null : dto.ExpectedResponse,
                      ta = string.IsNullOrWhiteSpace(dto.TamilHint) ? null : dto.TamilHint, id = turnId });
            return Ok(new { message = "Turn updated" });
        }

        // ── DELETE /api/conversation/turn/{turnId} (Admin) ────────────────
        [HttpDelete("turn/{turnId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTurn(int turnId)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("DELETE FROM conversation_turn WHERE turn_id=@id", new { id = turnId });
            return Ok(new { message = "Turn deleted" });
        }
    }

    public record ConversationDto(string Title, string? Scenario, string? Description, string? Level, int DisplayOrder, bool IsActive);
    public record TurnDto(int TurnOrder, string SystemText, string? ExpectedResponse, string? TamilHint);
}
