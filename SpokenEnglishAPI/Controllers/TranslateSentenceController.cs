using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Infrastructure.Data;
using System.Text;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/translate")]
    public class TranslateSentenceController : ControllerBase
    {
        private readonly DbContext _db;
        public TranslateSentenceController(DbContext db) => _db = db;

        // ── User-facing ──────────────────────────────────────────

        [HttpGet("{lessonId}/{languageId}")]
        public async Task<IActionResult> GetByLesson(int lessonId, int languageId)
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT id, lessonid, tamilsentence, correctsentence, displayorder
                  FROM translatesentence
                  WHERE lessonid = @lid AND isactive = true
                  ORDER BY displayorder, id",
                new { lid = lessonId });
            return Ok(rows);
        }

        // ── Admin CRUD ────────────────────────────────────────────

        [HttpGet("admin/{lessonId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetByLesson(int lessonId)
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT id, lessonid, tamilsentence, correctsentence, displayorder, isactive, createddate
                  FROM translatesentence WHERE lessonid = @lid ORDER BY displayorder, id",
                new { lid = lessonId });
            return Ok(rows);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromBody] TranslateSentenceUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TamilSentence) || string.IsNullOrWhiteSpace(dto.CorrectSentence))
                return BadRequest(new { message = "Both Tamil and English sentences are required" });
            using var con = _db.CreateConnection();
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO translatesentence (lessonid, tamilsentence, correctsentence, displayorder)
                  VALUES (@lid, @ta, @en, @ord) RETURNING id",
                new { lid = dto.LessonId, ta = dto.TamilSentence.Trim(), en = dto.CorrectSentence.Trim(), ord = dto.DisplayOrder });
            return Ok(new { id, message = "Added" });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] TranslateSentenceUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TamilSentence) || string.IsNullOrWhiteSpace(dto.CorrectSentence))
                return BadRequest(new { message = "Both Tamil and English sentences are required" });
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE translatesentence SET tamilsentence=@ta, correctsentence=@en, displayorder=@ord, isactive=@active
                  WHERE id=@id",
                new { ta = dto.TamilSentence.Trim(), en = dto.CorrectSentence.Trim(), ord = dto.DisplayOrder, active = dto.IsActive, id });
            return Ok(new { message = "Updated" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("DELETE FROM translatesentence WHERE id=@id", new { id });
            return Ok(new { message = "Deleted" });
        }

        // ── Import CSV ────────────────────────────────────────────

        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            using var reader = new StreamReader(file.OpenReadStream());
            using var con = _db.CreateConnection();
            int imported = 0, skipped = 0;
            bool firstLine = true;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (firstLine && line.StartsWith("LessonId", StringComparison.OrdinalIgnoreCase))
                { firstLine = false; continue; }
                firstLine = false;

                var cols = line.Split(',');
                if (cols.Length < 3) { skipped++; continue; }

                if (!int.TryParse(cols[0].Trim(), out int lessonId)) { skipped++; continue; }
                var tamil = cols[1].Trim().Trim('"');
                var english = cols[2].Trim().Trim('"');
                int order = cols.Length >= 4 && int.TryParse(cols[3].Trim(), out int o) ? o : 1;

                if (string.IsNullOrWhiteSpace(tamil) || string.IsNullOrWhiteSpace(english)) { skipped++; continue; }

                await con.ExecuteAsync(
                    @"INSERT INTO translatesentence (lessonid, tamilsentence, correctsentence, displayorder)
                      VALUES (@lid, @ta, @en, @ord)",
                    new { lid = lessonId, ta = tamil, en = english, ord = order });
                imported++;
            }

            return Ok(new { imported, skipped, message = $"Imported {imported} translate sentences" });
        }

        // ── Export CSV ────────────────────────────────────────────

        [HttpGet("export")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Export()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT ts.lessonid, ll.lessonname, ts.tamilsentence, ts.correctsentence, ts.displayorder
                  FROM translatesentence ts
                  JOIN lesson_lang ll ON ll.lessonid = ts.lessonid AND ll.languageid = 1
                  ORDER BY ts.lessonid, ts.displayorder, ts.id");

            var sb = new StringBuilder();
            sb.AppendLine("LessonId,LessonName,TamilSentence,CorrectSentence,DisplayOrder");
            foreach (var r in rows)
                sb.AppendLine($"{r.lessonid},\"{r.lessonname}\",\"{r.tamilsentence}\",\"{r.correctsentence}\",{r.displayorder}");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "translatesentences.csv");
        }
    }

    public record TranslateSentenceUpsertDto(
        int LessonId,
        string TamilSentence,
        string CorrectSentence,
        int DisplayOrder = 1,
        bool IsActive = true);
}
