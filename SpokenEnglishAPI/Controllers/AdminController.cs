using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly DbContext _db;
        public AdminController(DbContext db) => _db = db;

        // ── GET /api/admin/stats ─────────────────────────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            using var con = _db.CreateConnection();
            var users    = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
            var lessons  = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM lesson");
            var subs     = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM user_subscriptions WHERE status='active'");
            var premium  = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE role='Admin'");
            return Ok(new { totalUsers = users, totalLessons = lessons, activeSubs = subs, admins = premium });
        }

        // ── GET /api/admin/users ─────────────────────────────────────────────
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT u.id, u.email, u.mobilenumber, u.role, u.isactive, u.createddate,
                         us.planname, us.enddate, us.status as substatus
                  FROM users u
                  LEFT JOIN user_subscriptions us ON us.user_id = u.id AND us.status='active'
                  ORDER BY u.id DESC LIMIT 200");
            return Ok(rows);
        }

        // ── POST /api/admin/users/{id}/role ──────────────────────────────────
        [HttpPost("users/{id}/role")]
        public async Task<IActionResult> SetRole(int id, [FromBody] RoleDto dto)
        {
            var allowed = new[] { "Admin", "User", "Premium" };
            if (!allowed.Contains(dto.Role)) return BadRequest("Invalid role");
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("UPDATE users SET role=@role WHERE id=@id", new { role = dto.Role, id });
            return Ok(new { message = "Role updated" });
        }

        // ── POST /api/admin/users/{id}/toggle ────────────────────────────────
        [HttpPost("users/{id}/toggle")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("UPDATE users SET isactive = NOT isactive WHERE id=@id", new { id });
            return Ok(new { message = "Status toggled" });
        }

        // ── GET /api/admin/export/lessons ────────────────────────────────────
        [HttpGet("export/lessons")]
        public async Task<IActionResult> ExportLessons([FromQuery] int languageId = 1)
        {
            using var con = _db.CreateConnection();
            var lessons = await con.QueryAsync(
                @"SELECT l.lessonid, ll.lessonname, ll.description, lt.typename,
                         l.lessonorder, l.isactive, l.is_premium
                  FROM lesson l
                  JOIN lesson_lang ll ON ll.lessonid=l.lessonid AND ll.languageid=@lid
                  LEFT JOIN lessontype lt ON lt.lessontypeid=l.lessontypeid
                  ORDER BY l.lessonorder", new { lid = languageId });

            var csv = "LessonId,LessonName,Description,TypeName,LessonOrder,IsActive,IsPremium\n" +
                string.Join("\n", lessons.Select(r =>
                    $"{r.lessonid},\"{r.lessonname}\",\"{r.description}\",\"{r.typename}\",{r.lessonorder},{r.isactive},{r.is_premium}"));

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "lessons.csv");
        }

        // ── GET /api/admin/export/wordcontent ────────────────────────────────
        [HttpGet("export/wordcontent")]
        public async Task<IActionResult> ExportWordContent()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT wc.content_id, wc.lesson_id, ll.lessonname, wc.word_name,
                         wc.sentence_pattern, wc.definition_en, wc.definition_ta,
                         wc.example_en, wc.example_ta, wc.display_order
                  FROM lesson_word_content wc
                  JOIN lesson_lang ll ON ll.lessonid=wc.lesson_id AND ll.languageid=1
                  ORDER BY wc.lesson_id, wc.display_order");

            var csv = "ContentId,LessonId,LessonName,WordName,SentencePattern,DefinitionEn,DefinitionTa,ExampleEn,ExampleTa,DisplayOrder\n" +
                string.Join("\n", rows.Select(r =>
                    $"{r.content_id},{r.lesson_id},\"{r.lessonname}\",\"{r.word_name}\",\"{r.sentence_pattern}\",\"{r.definition_en}\",\"{r.definition_ta}\",\"{r.example_en}\",\"{r.example_ta}\",{r.display_order}"));

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "word_content.csv");
        }

        // ── GET /api/admin/export/mcq ─────────────────────────────────────────
        [HttpGet("export/mcq")]
        public async Task<IActionResult> ExportMcq()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT q.questionid, q.lessonid, ll.lessonname, q.questiontext,
                         o.optionid, o.optiontext, o.iscorrect
                  FROM meaningquestion q
                  JOIN lesson_lang ll ON ll.lessonid=q.lessonid AND ll.languageid=1
                  JOIN meaningoption o ON o.questionid=q.questionid
                  ORDER BY q.lessonid, q.questionid, o.iscorrect DESC");

            var csv = "QuestionId,LessonId,LessonName,QuestionText,OptionId,OptionText,IsCorrect\n" +
                string.Join("\n", rows.Select(r =>
                    $"{r.questionid},{r.lessonid},\"{r.lessonname}\",\"{r.questiontext}\",{r.optionid},\"{r.optiontext}\",{r.iscorrect}"));

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "mcq.csv");
        }

        // ── POST /api/admin/import/wordcontent ───────────────────────────────
        [HttpPost("import/wordcontent")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportWordContent(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file");
            if (!file.FileName.EndsWith(".csv")) return BadRequest("CSV only");

            using var reader = new System.IO.StreamReader(file.OpenReadStream());
            var lines = new List<string>();
            while (!reader.EndOfStream) lines.Add(await reader.ReadLineAsync() ?? "");
            if (lines.Count < 2) return BadRequest("Empty file");

            using var con = _db.CreateConnection();
            int imported = 0, skipped = 0;

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = ParseCsvLine(line);
                if (cols.Length < 10) { skipped++; continue; }

                try
                {
                    if (!int.TryParse(cols[1], out int lessonId)) { skipped++; continue; }
                    await con.ExecuteAsync(
                        @"INSERT INTO lesson_word_content
                            (lesson_id, word_name, sentence_pattern, definition_en, definition_ta, example_en, example_ta, display_order)
                          VALUES (@lid, @wn, @sp, @den, @dta, @een, @eta, @ord)
                          ON CONFLICT DO NOTHING",
                        new { lid=lessonId, wn=cols[3], sp=cols[4], den=cols[5], dta=cols[6], een=cols[7], eta=cols[8], ord=int.TryParse(cols[9],out int o)?o:0 });
                    imported++;
                }
                catch { skipped++; }
            }

            return Ok(new { imported, skipped });
        }

        // ── POST /api/admin/lessons/{id}/premium ─────────────────────────────
        [HttpPost("lessons/{id}/premium")]
        public async Task<IActionResult> SetPremium(int id, [FromBody] PremiumDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("UPDATE lesson SET is_premium=@p WHERE lessonid=@id", new { p = dto.IsPremium, id });
            return Ok(new { message = dto.IsPremium ? "Lesson set as premium" : "Lesson set as free" });
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuote = false;
            var current = new System.Text.StringBuilder();
            foreach (char c in line)
            {
                if (c == '"') { inQuote = !inQuote; }
                else if (c == ',' && !inQuote) { result.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
    }

    public record RoleDto(string Role);
    public record PremiumDto(bool IsPremium);
}
