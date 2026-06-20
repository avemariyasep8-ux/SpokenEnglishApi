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

        // ── POST /api/admin/users/{id}/grant-access ──────────────────────────
        [HttpPost("users/{id}/grant-access")]
        public async Task<IActionResult> GrantAccess(int id, [FromBody] GrantAccessDto dto)
        {
            using var con = _db.CreateConnection();
            var role = dto.Grant ? "Premium" : "User";
            await con.ExecuteAsync("UPDATE users SET role=@role WHERE id=@id", new { role, id });
            return Ok(new { message = dto.Grant ? "Premium access granted" : "Premium access revoked" });
        }

        // ── GET /api/admin/template/{type} ───────────────────────────────────
        [HttpGet("template/{type}")]
        public IActionResult GetTemplate(string type)
        {
            var csv = type switch
            {
                "lessons" =>
                    "LessonOrder,LessonName,Description,IsPremium\n" +
                    "1,Greetings,Learn basic English greetings and introductions,false\n" +
                    "2,Daily Phrases,Common phrases used every day,false\n" +
                    "3,IS - Grammar,Learn when and how to use IS,false\n",
                "wordcontent" =>
                    "LessonId,WordName,SentencePattern,DefinitionEn,DefinitionTa,ExampleEn,ExampleTa\n" +
                    "1,Hello,Subject + Hello,A common English greeting,ஒரு பொதுவான வணக்கம்,Hello! How are you?,வணக்கம்! நீங்கள் எப்படி இருக்கிறீர்கள்?\n" +
                    "1,Good morning,Subject + Good morning,A greeting used in the morning,காலை வணக்கம்,Good morning! Have a nice day!,காலை வணக்கம்! நல்ல நாளாக இருக்கட்டும்!\n",
                "mcq" =>
                    "LessonId,QuestionText,Option1,Option2,Option3,Option4,CorrectOption\n" +
                    "1,She ___ a student.,is,are,am,be,1\n" +
                    "1,The sky ___ blue.,are,is,am,were,2\n",
                "arrange" =>
                    "LessonId,Sentence,HintText\n" +
                    "1,She is happy.,Subject + is + Adjective\n" +
                    "1,He is a teacher.,Subject + is + a + Noun\n",
                _ => null
            };
            if (csv == null) return NotFound("Unknown template type");
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"{type}_template.csv");
        }

        // ── POST /api/admin/import/lessons ───────────────────────────────────
        [HttpPost("import/lessons")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportLessons(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file");
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
                if (cols.Length < 4) { skipped++; continue; }
                try
                {
                    if (!int.TryParse(cols[0], out int order)) { skipped++; continue; }
                    var lessonName = cols[1].Trim();
                    var description = cols[2].Trim();
                    bool.TryParse(cols[3].Trim().ToLower(), out bool isPremium);

                    var existing = await con.ExecuteScalarAsync<int?>(
                        "SELECT lessonid FROM lesson WHERE lessonorder=@order", new { order });
                    int lessonId;
                    if (existing.HasValue)
                    {
                        lessonId = existing.Value;
                        await con.ExecuteAsync(
                            "UPDATE lesson SET is_premium=@p WHERE lessonid=@id",
                            new { p = isPremium, id = lessonId });
                        await con.ExecuteAsync(
                            "UPDATE lesson_lang SET lessonname=@n, description=@d WHERE lessonid=@id AND languageid=1",
                            new { n = lessonName, d = description, id = lessonId });
                    }
                    else
                    {
                        lessonId = await con.ExecuteScalarAsync<int>(
                            @"INSERT INTO lesson (lessontypeid, lessonorder, isactive, is_premium)
                              VALUES (1, @order, true, @p) RETURNING lessonid",
                            new { order, p = isPremium });
                        await con.ExecuteAsync(
                            @"INSERT INTO lesson_lang (lessonid, languageid, lessonname, description)
                              VALUES (@id, 1, @n, @d)
                              ON CONFLICT (lessonid, languageid) DO UPDATE SET lessonname=@n, description=@d",
                            new { id = lessonId, n = lessonName, d = description });
                    }
                    imported++;
                }
                catch { skipped++; }
            }
            return Ok(new { imported, skipped, message = $"Imported {imported} lessons, skipped {skipped}" });
        }

        // ── POST /api/admin/import/mcq ────────────────────────────────────────
        [HttpPost("import/mcq")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportMcq(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file");
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
                if (cols.Length < 7) { skipped++; continue; }
                try
                {
                    if (!int.TryParse(cols[0], out int lessonId)) { skipped++; continue; }
                    var questionText = cols[1].Trim();
                    var options = new[] { cols[2].Trim(), cols[3].Trim(), cols[4].Trim(), cols[5].Trim() };
                    if (!int.TryParse(cols[6].Trim(), out int correctIdx) || correctIdx < 1 || correctIdx > 4) { skipped++; continue; }

                    var qId = await con.ExecuteScalarAsync<int>(
                        "INSERT INTO meaningquestion (lessonid) VALUES (@lid) RETURNING questionid",
                        new { lid = lessonId });
                    await con.ExecuteAsync(
                        "INSERT INTO meaningquestion_lang (questionid, languageid, questiontext) VALUES (@qid, 1, @qt)",
                        new { qid = qId, qt = questionText });

                    for (int i = 0; i < options.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(options[i])) continue;
                        var optId = await con.ExecuteScalarAsync<int>(
                            "INSERT INTO meaningoption (questionid, iscorrect) VALUES (@qid, @correct) RETURNING optionid",
                            new { qid = qId, correct = (i + 1) == correctIdx });
                        await con.ExecuteAsync(
                            "INSERT INTO meaningoption_lang (optionid, languageid, optiontext) VALUES (@oid, 1, @ot)",
                            new { oid = optId, ot = options[i] });
                    }
                    imported++;
                }
                catch { skipped++; }
            }
            return Ok(new { imported, skipped, message = $"Imported {imported} questions, skipped {skipped}" });
        }

        // ── POST /api/admin/import/arrange ────────────────────────────────────
        [HttpPost("import/arrange")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportArrange(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file");
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
                if (cols.Length < 2) { skipped++; continue; }
                try
                {
                    if (!int.TryParse(cols[0], out int lessonId)) { skipped++; continue; }
                    var sentence = cols[1].Trim();
                    if (string.IsNullOrWhiteSpace(sentence)) { skipped++; continue; }

                    var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // Insert into arrangesentence
                    var arrId = await con.ExecuteScalarAsync<int>(
                        "INSERT INTO arrangesentence (lessonid) VALUES (@lid) RETURNING arrangesentenceid",
                        new { lid = lessonId });

                    // Insert sentence text
                    await con.ExecuteAsync(
                        "INSERT INTO arrangesentence_lang (arrangesentenceid, languageid, correctsentence) VALUES (@aid, 1, @s)",
                        new { aid = arrId, s = sentence });

                    // Insert each word
                    for (int i = 0; i < words.Length; i++)
                    {
                        var wid = await con.ExecuteScalarAsync<int>(
                            "INSERT INTO arrangesentenceword (arrangesentenceid, correctorder) VALUES (@aid, @ord) RETURNING wordid",
                            new { aid = arrId, ord = i + 1 });
                        await con.ExecuteAsync(
                            "INSERT INTO arrangesentenceword_lang (wordid, languageid, wordtext) VALUES (@wid, 1, @w)",
                            new { wid, w = words[i] });
                    }
                    imported++;
                }
                catch { skipped++; }
            }
            return Ok(new { imported, skipped, message = $"Imported {imported} sentences, skipped {skipped}" });
        }

        // ── GET /api/admin/lesson-stats ───────────────────────────────────────
        [HttpGet("lesson-stats")]
        public async Task<IActionResult> GetLessonStats()
        {
            using var con = _db.CreateConnection();
            var stats = await con.QueryAsync(
                @"SELECT l.lessonid,
                    (SELECT COUNT(*) FROM lesson_word_content wc WHERE wc.lesson_id = l.lessonid) as wordcount,
                    (SELECT COUNT(*) FROM meaningquestion mq WHERE mq.lessonid = l.lessonid) as mcqcount,
                    (SELECT COUNT(*) FROM arrangesentence ar WHERE ar.lessonid = l.lessonid) as arrangecount
                  FROM lesson l ORDER BY l.lessonorder");
            return Ok(stats);
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
    public record GrantAccessDto(bool Grant);
}
