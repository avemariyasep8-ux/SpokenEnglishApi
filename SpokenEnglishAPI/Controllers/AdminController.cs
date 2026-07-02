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
                         sp.plan_name as planname, us.end_date as enddate, us.status as substatus
                  FROM users u
                  LEFT JOIN user_subscriptions us ON us.user_id = u.id AND us.status='active'
                  LEFT JOIN subscription_plans sp ON sp.plan_id = us.plan_id
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
                         l.lessonorder, l.isactive, l.is_premium, COALESCE(l.level,'Beginner') AS level
                  FROM lesson l
                  JOIN lesson_lang ll ON ll.lessonid=l.lessonid AND ll.languageid=@lid
                  LEFT JOIN lessontype lt ON lt.lessontypeid=l.lessontypeid
                  ORDER BY l.lessonorder", new { lid = languageId });

            var csv = "LessonId,LessonName,Description,TypeName,LessonOrder,IsActive,IsPremium,Level\n" +
                string.Join("\n", lessons.Select(r =>
                    $"{r.lessonid},\"{r.lessonname}\",\"{r.description}\",\"{r.typename}\",{r.lessonorder},{r.isactive},{r.is_premium},\"{r.level}\""));

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

        // ── GET /api/admin/export/arrange ────────────────────────────────────
        [HttpGet("export/arrange")]
        public async Task<IActionResult> ExportArrange()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT a.arrangesentenceid, a.lessonid, ll.lessonname,
                         al.correctsentence, al.tamilmeaning
                  FROM arrangesentence a
                  JOIN arrangesentence_lang al ON al.arrangesentenceid=a.arrangesentenceid AND al.languageid=1
                  JOIN lesson_lang ll ON ll.lessonid=a.lessonid AND ll.languageid=1
                  ORDER BY a.lessonid, a.arrangesentenceid");

            var csv = "ArrangeSentenceId,LessonId,LessonName,CorrectSentence,TamilMeaning\n" +
                string.Join("\n", rows.Select(r =>
                    $"{r.arrangesentenceid},{r.lessonid},\"{r.lessonname}\",\"{r.correctsentence}\",\"{r.tamilmeaning}\""));

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "arrange_sentences.csv");
        }

        // ── POST /api/admin/import/wordcontent ───────────────────────────────
        // Template format (7 cols): LessonId, WordName, SentencePattern, DefinitionEn, DefinitionTa, ExampleEn, ExampleTa
        // Export format (10 cols):  ContentId, LessonId, LessonName, WordName, SentencePattern, DefinitionEn, DefinitionTa, ExampleEn, ExampleTa, DisplayOrder
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

            // Detect format from header row
            var header = lines[0].Split(',');
            bool isExportFormat = header.Length >= 10 &&
                header[0].Trim().Equals("ContentId", StringComparison.OrdinalIgnoreCase);

            using var con = _db.CreateConnection();
            int imported = 0, skipped = 0;

            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = ParseCsvLine(line);
                try
                {
                    int lessonId; string wn, sp, den, dta, een, eta; int ord;
                    if (isExportFormat)
                    {
                        // ContentId(0), LessonId(1), LessonName(2), WordName(3), SentencePattern(4), DefinitionEn(5), DefinitionTa(6), ExampleEn(7), ExampleTa(8), DisplayOrder(9)
                        if (cols.Length < 10) { skipped++; continue; }
                        if (!int.TryParse(cols[1], out lessonId)) { skipped++; continue; }
                        wn = cols[3]; sp = cols[4]; den = cols[5]; dta = cols[6]; een = cols[7]; eta = cols[8];
                        int.TryParse(cols[9], out ord);
                    }
                    else
                    {
                        // Template: LessonId(0), WordName(1), SentencePattern(2), DefinitionEn(3), DefinitionTa(4), ExampleEn(5), ExampleTa(6), DisplayOrder(7 optional)
                        if (cols.Length < 7) { skipped++; continue; }
                        if (!int.TryParse(cols[0], out lessonId)) { skipped++; continue; }
                        wn = cols[1]; sp = cols[2]; den = cols[3]; dta = cols[4]; een = cols[5]; eta = cols[6];
                        ord = cols.Length >= 8 && int.TryParse(cols[7], out int o2) ? o2 : 0;
                    }

                    if (string.IsNullOrWhiteSpace(wn)) { skipped++; continue; }
                    await con.ExecuteAsync(
                        @"INSERT INTO lesson_word_content
                            (lesson_id, word_name, sentence_pattern, definition_en, definition_ta, example_en, example_ta, display_order)
                          VALUES (@lid, @wn, @sp, @den, @dta, @een, @eta, @ord)
                          ON CONFLICT DO NOTHING",
                        new { lid=lessonId, wn, sp, den, dta, een, eta, ord });
                    imported++;
                }
                catch { skipped++; }
            }

            return Ok(new { imported, skipped, message = $"Imported {imported} rows, skipped {skipped}" });
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
                    "LessonOrder,LessonName,Description,IsPremium,Level\n" +
                    "1,Greetings,Learn basic English greetings and introductions,false,Beginner\n" +
                    "2,Daily Phrases,Common phrases used every day,false,Elementary\n" +
                    "3,IS - Grammar,Learn when and how to use IS,false,Intermediate\n",
                "wordcontent" =>
                    "LessonId,WordName,SentencePattern,DefinitionEn,DefinitionTa,ExampleEn,ExampleTa,DisplayOrder\n" +
                    "1,Hello,Subject + Hello,A common English greeting,ஒரு பொதுவான வணக்கம்,Hello! How are you?,வணக்கம்! நீங்கள் எப்படி இருக்கிறீர்கள்?,1\n" +
                    "1,Good morning,Subject + Good morning,A greeting used in the morning,காலை வணக்கம்,Good morning! Have a nice day!,காலை வணக்கம்! நல்ல நாளாக இருக்கட்டும்!,2\n",
                "mcq" =>
                    "LessonId,QuestionText,Option1,Option2,Option3,Option4,CorrectOption\n" +
                    "1,She ___ a student.,is,are,am,be,1\n" +
                    "1,The sky ___ blue.,are,is,am,were,2\n",
                "arrange" =>
                    "LessonId,CorrectSentence,TamilMeaning\n" +
                    "1,She is happy.,அவள் மகிழ்ச்சியாக இருக்கிறாள்.\n" +
                    "1,He is a teacher.,அவர் ஒரு ஆசிரியர்.\n",
                "translate" =>
                    "LessonId,CorrectSentence,TamilMeaning\n" +
                    "1,I drink water every day.,நான் தினமும் தண்ணீர் குடிக்கிறேன்.\n" +
                    "1,She goes to school.,அவள் பள்ளிக்கு செல்கிறாள்.\n",
                "reading" =>
                    "LessonId,SentenceText,DisplayOrder\n" +
                    "1,The sun rises in the east.,1\n" +
                    "1,Birds fly in the sky.,2\n",
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

                    // Optional Level column (5th). Defaults to Beginner; validated against allowed set.
                    var allowedLevels = new[] { "Beginner", "Elementary", "Intermediate", "College", "Professional" };
                    var level = cols.Length >= 5 ? cols[4].Trim() : "Beginner";
                    if (string.IsNullOrWhiteSpace(level) || !allowedLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
                        level = "Beginner";

                    var existing = await con.ExecuteScalarAsync<int?>(
                        "SELECT lessonid FROM lesson WHERE lessonorder=@order", new { order });
                    int lessonId;
                    if (existing.HasValue)
                    {
                        lessonId = existing.Value;
                        await con.ExecuteAsync(
                            "UPDATE lesson SET is_premium=@p, level=@lvl WHERE lessonid=@id",
                            new { p = isPremium, lvl = level, id = lessonId });
                        await con.ExecuteAsync(
                            "UPDATE lesson_lang SET lessonname=@n, description=@d WHERE lessonid=@id AND languageid=1",
                            new { n = lessonName, d = description, id = lessonId });
                    }
                    else
                    {
                        lessonId = await con.ExecuteScalarAsync<int>(
                            @"INSERT INTO lesson (lessontypeid, lessonorder, isactive, is_premium, level)
                              VALUES (1, @order, true, @p, @lvl) RETURNING lessonid",
                            new { order, p = isPremium, lvl = level });
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
        // CSV format: LessonId, CorrectSentence, TamilMeaning (optional), Words pipe-separated (optional)
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

                    var tamilMeaning = cols.Length >= 3 ? cols[2].Trim() : null;

                    // Words may be pipe-separated in col[3], else split from sentence
                    string[] words;
                    if (cols.Length >= 4 && !string.IsNullOrWhiteSpace(cols[3]))
                        words = cols[3].Split('|', StringSplitOptions.RemoveEmptyEntries);
                    else
                        words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var arrId = await con.ExecuteScalarAsync<int>(
                        "INSERT INTO arrangesentence (lessonid) VALUES (@lid) RETURNING arrangesentenceid",
                        new { lid = lessonId });

                    await con.ExecuteAsync(
                        "INSERT INTO arrangesentence_lang (arrangesentenceid, languageid, correctsentence, tamilmeaning) VALUES (@aid, 1, @s, @tm)",
                        new { aid = arrId, s = sentence, tm = string.IsNullOrWhiteSpace(tamilMeaning) ? null : tamilMeaning });

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

        // ── POST /api/admin/users/{id}/password ──────────────────────────────
        [HttpPost("users/{id}/password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] AdminResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters" });
            using var con = _db.CreateConnection();
            var exists = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE id=@id", new { id });
            if (exists == 0) return NotFound(new { message = "User not found" });
            var hash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await con.ExecuteAsync("UPDATE users SET passwordhash=@hash, modifydate=NOW() WHERE id=@id", new { hash, id });
            return Ok(new { message = "Password updated" });
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
                    (SELECT COUNT(*) FROM arrangesentence ar WHERE ar.lessonid = l.lessonid) as arrangecount,
                    (SELECT COUNT(*) FROM readingsentence rs WHERE rs.lessonid = l.lessonid) as readingcount,
                    (SELECT COUNT(*) FROM arrangesentence a
                       JOIN arrangesentence_lang al ON al.arrangesentenceid=a.arrangesentenceid AND al.languageid=1
                       WHERE a.lessonid=l.lessonid AND al.tamilmeaning IS NOT NULL AND al.tamilmeaning <> '') as translatecount
                  FROM lesson l ORDER BY l.lessonorder");
            return Ok(stats);
        }

        // ── GET /api/admin/audit-log ──────────────────────────────────────
        // View recent security audit events (login/register/etc.). Admin only.
        [HttpGet("audit-log")]
        public async Task<IActionResult> GetAuditLog([FromQuery] int limit = 200, [FromQuery] string? action = null)
        {
            if (limit < 1 || limit > 1000) limit = 200;
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT id, user_id, email, action, ip_address, user_agent, success, detail, created_at
                  FROM audit_log
                  WHERE (@action IS NULL OR action = @action)
                  ORDER BY created_at DESC
                  LIMIT @limit",
                new { action, limit });
            return Ok(rows);
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

        // ── POST /api/admin/bulk/wordcontent ──────────────────────────────
        [HttpPost("bulk/wordcontent")]
        public async Task<IActionResult> BulkWordContent([FromBody] BulkWordContentDto dto)
        {
            using var con = _db.CreateConnection();
            int inserted = 0;
            foreach (var row in dto.Rows)
            {
                try
                {
                    await con.ExecuteAsync(
                        @"INSERT INTO lesson_word_content (lesson_id, word_name, sentence_pattern, definition_en, definition_ta, example_en, example_ta, display_order)
                          VALUES (@lid, @word, @pattern, @den, @dta, @een, @eta, @order)",
                        new { lid = dto.LessonId, word = row.WordName, pattern = row.SentencePattern,
                              den = row.DefinitionEn, dta = row.DefinitionTa, een = row.ExampleEn,
                              eta = row.ExampleTa, order = row.DisplayOrder });
                    inserted++;
                }
                catch { }
            }
            return Ok(new { inserted, message = $"{inserted} rows inserted" });
        }

        // ── POST /api/admin/bulk/mcq ──────────────────────────────────────
        [HttpPost("bulk/mcq")]
        public async Task<IActionResult> BulkMcq([FromBody] BulkMcqDto dto)
        {
            using var con = _db.CreateConnection();
            int inserted = 0;
            foreach (var q in dto.Questions)
            {
                try
                {
                    var qid = await con.ExecuteScalarAsync<int>(
                        "INSERT INTO meaningquestion (lessonid) VALUES (@lid) RETURNING questionid", new { lid = dto.LessonId });
                    await con.ExecuteAsync(
                        "INSERT INTO meaningquestion_lang (questionid, languageid, questiontext) VALUES (@qid, 1, @text)",
                        new { qid, text = q.QuestionText });
                    var opts = new[] {
                        (q.Option1, q.CorrectOption == 1),
                        (q.Option2, q.CorrectOption == 2),
                        (q.Option3, q.CorrectOption == 3),
                        (q.Option4, q.CorrectOption == 4)
                    };
                    foreach (var (text, isCorrect) in opts)
                    {
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        var oid = await con.ExecuteScalarAsync<int>(
                            "INSERT INTO meaningoption (questionid, iscorrect) VALUES (@qid, @c) RETURNING optionid",
                            new { qid, c = isCorrect });
                        await con.ExecuteAsync(
                            "INSERT INTO meaningoption_lang (optionid, languageid, optiontext) VALUES (@oid, 1, @t)",
                            new { oid, t = text });
                    }
                    inserted++;
                }
                catch { }
            }
            return Ok(new { inserted, message = $"{inserted} questions inserted" });
        }

        // ── POST /api/admin/bulk/fillin ───────────────────────────────────
        [HttpPost("bulk/fillin")]
        public async Task<IActionResult> BulkFillIn([FromBody] BulkFillInDto dto)
        {
            using var con = _db.CreateConnection();
            int inserted = 0;
            foreach (var row in dto.Rows)
            {
                try
                {
                    await con.ExecuteAsync(
                        @"INSERT INTO fillinblank (lessonid, sentence_with_blank, correct_answer, option1, option2, option3, hint_ta, display_order)
                          VALUES (@lid, @sentence, @answer, @o1, @o2, @o3, @hint, @order)",
                        new { lid = dto.LessonId, sentence = row.SentenceWithBlank, answer = row.CorrectAnswer,
                              o1 = row.Option1, o2 = row.Option2, o3 = row.Option3, hint = row.HintTa, order = row.DisplayOrder });
                    inserted++;
                }
                catch { }
            }
            return Ok(new { inserted, message = $"{inserted} rows inserted" });
        }

        // ── POST /api/admin/bulk/arrange ──────────────────────────────────
        [HttpPost("bulk/arrange")]
        public async Task<IActionResult> BulkArrange([FromBody] BulkArrangeDto dto)
        {
            using var con = _db.CreateConnection();
            int inserted = 0;
            foreach (var sentence in dto.Sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence)) continue;
                try
                {
                    var aid = await con.ExecuteScalarAsync<int>(
                        "INSERT INTO arrangesentence (lessonid) VALUES (@lid) RETURNING arrangesentenceid", new { lid = dto.LessonId });
                    await con.ExecuteAsync(
                        "INSERT INTO arrangesentence_lang (arrangesentenceid, languageid, correctsentence) VALUES (@aid, 1, @s)",
                        new { aid, s = sentence });
                    var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < words.Length; i++)
                    {
                        var wid = await con.ExecuteScalarAsync<int>(
                            "INSERT INTO arrangesentenceword (arrangesentenceid, correctorder) VALUES (@aid, @ord) RETURNING wordid",
                            new { aid, ord = i + 1 });
                        await con.ExecuteAsync(
                            "INSERT INTO arrangesentenceword_lang (wordid, languageid, wordtext) VALUES (@wid, 1, @w)",
                            new { wid, w = words[i] });
                    }
                    inserted++;
                }
                catch { }
            }
            return Ok(new { inserted, message = $"{inserted} sentences inserted" });
        }

        // ── POST /api/admin/users ─────────────────────────────────────────
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserAdminDto dto)
        {
            using var con = _db.CreateConnection();
            var exists = await con.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM users WHERE email=@email", new { email = dto.Email });
            if (exists > 0) return BadRequest(new { message = "Email already exists" });

            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var apiKey = Guid.NewGuid().ToString("N");
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO users (email, mobilenumber, passwordhash, apikey, role, isactive)
                  VALUES (@email, @mobile, @hash, @apikey, @role, @active) RETURNING id",
                new { email = dto.Email, mobile = dto.Mobile, hash, apikey = apiKey,
                      role = dto.Role ?? "User", active = true });
            return Ok(new { id, message = "User created" });
        }

        // ── PUT /api/admin/users/{id} ─────────────────────────────────────
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserAdminDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE users SET email=@email, mobilenumber=@mobile, role=@role, isactive=@active
                  WHERE id=@id",
                new { email = dto.Email, mobile = dto.Mobile, role = dto.Role, active = dto.IsActive, id });
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await con.ExecuteAsync("UPDATE users SET passwordhash=@hash WHERE id=@id", new { hash, id });
            }
            return Ok(new { message = "User updated" });
        }

        // ── DELETE /api/admin/users/{id} ──────────────────────────────────
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("UPDATE users SET isactive=false WHERE id=@id", new { id });
            return Ok(new { message = "User deactivated" });
        }

        // ── GET /api/admin/lesson-content/{lessonId} ──────────────────────
        [HttpGet("lesson-content/{lessonId}")]
        public async Task<IActionResult> GetLessonContent(int lessonId)
        {
            using var con = _db.CreateConnection();
            var wordContent = await con.QueryAsync(
                @"SELECT content_id as id, word_name, sentence_pattern, definition_en, definition_ta,
                         example_en, example_ta, display_order
                  FROM lesson_word_content WHERE lesson_id=@lid ORDER BY display_order",
                new { lid = lessonId });

            var mcq = await con.QueryAsync(
                @"SELECT q.questionid as id, mql.questiontext,
                         json_agg(json_build_object('optionid', o.optionid, 'optiontext', ol.optiontext, 'iscorrect', o.iscorrect)
                                  ORDER BY o.optionid) as options
                  FROM meaningquestion q
                  JOIN meaningquestion_lang mql ON mql.questionid=q.questionid AND mql.languageid=1
                  JOIN meaningoption o ON o.questionid=q.questionid
                  JOIN meaningoption_lang ol ON ol.optionid=o.optionid AND ol.languageid=1
                  WHERE q.lessonid=@lid
                  GROUP BY q.questionid, mql.questiontext ORDER BY q.questionid",
                new { lid = lessonId });

            var fillin = await con.QueryAsync(
                @"SELECT id, sentence_with_blank, correct_answer, option1, option2, option3, hint_ta, display_order
                  FROM fillinblank WHERE lessonid=@lid ORDER BY display_order",
                new { lid = lessonId });

            var arrange = await con.QueryAsync(
                @"SELECT a.arrangesentenceid as id, al.correctsentence,
                         json_agg(json_build_object('wordid', w.wordid, 'wordtext', wl.wordtext, 'correctorder', w.correctorder)
                                  ORDER BY w.correctorder) as words
                  FROM arrangesentence a
                  JOIN arrangesentence_lang al ON al.arrangesentenceid=a.arrangesentenceid AND al.languageid=1
                  JOIN arrangesentenceword w ON w.arrangesentenceid=a.arrangesentenceid
                  JOIN arrangesentenceword_lang wl ON wl.wordid=w.wordid AND wl.languageid=1
                  WHERE a.lessonid=@lid
                  GROUP BY a.arrangesentenceid, al.correctsentence ORDER BY a.arrangesentenceid",
                new { lid = lessonId });

            return Ok(new { wordContent, mcq, fillin, arrange });
        }

        // ── POST /api/admin/wordcontent ───────────────────────────────────
        [HttpPost("wordcontent")]
        public async Task<IActionResult> AddWordContent([FromBody] WordContentDto dto)
        {
            using var con = _db.CreateConnection();
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO lesson_word_content (lesson_id, word_name, sentence_pattern, definition_en, definition_ta, example_en, example_ta, display_order)
                  VALUES (@lid, @word, @pattern, @den, @dta, @een, @eta, @order) RETURNING content_id",
                new { lid = dto.LessonId, word = dto.WordName, pattern = dto.SentencePattern,
                      den = dto.DefinitionEn, dta = dto.DefinitionTa, een = dto.ExampleEn,
                      eta = dto.ExampleTa, order = dto.DisplayOrder });
            return Ok(new { id, message = "Word content added" });
        }

        // ── PUT /api/admin/wordcontent/{id} ───────────────────────────────
        [HttpPut("wordcontent/{id}")]
        public async Task<IActionResult> UpdateWordContent(int id, [FromBody] WordContentDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE lesson_word_content SET word_name=@word, sentence_pattern=@pattern,
                  definition_en=@den, definition_ta=@dta, example_en=@een, example_ta=@eta, display_order=@order
                  WHERE content_id=@id",
                new { word = dto.WordName, pattern = dto.SentencePattern, den = dto.DefinitionEn,
                      dta = dto.DefinitionTa, een = dto.ExampleEn, eta = dto.ExampleTa,
                      order = dto.DisplayOrder, id });
            return Ok(new { message = "Updated" });
        }

        // ── DELETE /api/admin/wordcontent/{id} ────────────────────────────
        [HttpDelete("wordcontent/{id}")]
        public async Task<IActionResult> DeleteWordContent(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("DELETE FROM lesson_word_content WHERE content_id=@id", new { id });
            return Ok(new { message = "Deleted" });
        }

        // ── POST /api/admin/mcq ───────────────────────────────────────────
        [HttpPost("mcq")]
        public async Task<IActionResult> AddMcq([FromBody] McqDto dto)
        {
            using var con = _db.CreateConnection();
            var qid = await con.ExecuteScalarAsync<int>(
                "INSERT INTO meaningquestion (lessonid) VALUES (@lid) RETURNING questionid",
                new { lid = dto.LessonId });
            await con.ExecuteAsync(
                "INSERT INTO meaningquestion_lang (questionid, languageid, questiontext) VALUES (@qid, 1, @text)",
                new { qid, text = dto.QuestionText });
            foreach (var opt in dto.Options)
            {
                var oid = await con.ExecuteScalarAsync<int>(
                    "INSERT INTO meaningoption (questionid, iscorrect) VALUES (@qid, @correct) RETURNING optionid",
                    new { qid, correct = opt.IsCorrect });
                await con.ExecuteAsync(
                    "INSERT INTO meaningoption_lang (optionid, languageid, optiontext) VALUES (@oid, 1, @text)",
                    new { oid, text = opt.OptionText });
            }
            return Ok(new { id = qid, message = "MCQ added" });
        }

        // ── PUT /api/admin/mcq/{id} ───────────────────────────────────────
        [HttpPut("mcq/{id}")]
        public async Task<IActionResult> UpdateMcq(int id, [FromBody] McqDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                "UPDATE meaningquestion_lang SET questiontext=@text WHERE questionid=@id AND languageid=1",
                new { text = dto.QuestionText, id });
            await con.ExecuteAsync(
                "DELETE FROM meaningoption_lang WHERE optionid IN (SELECT optionid FROM meaningoption WHERE questionid=@id); DELETE FROM meaningoption WHERE questionid=@id",
                new { id });
            foreach (var opt in dto.Options)
            {
                var oid = await con.ExecuteScalarAsync<int>(
                    "INSERT INTO meaningoption (questionid, iscorrect) VALUES (@qid, @correct) RETURNING optionid",
                    new { qid = id, correct = opt.IsCorrect });
                await con.ExecuteAsync(
                    "INSERT INTO meaningoption_lang (optionid, languageid, optiontext) VALUES (@oid, 1, @text)",
                    new { oid, text = opt.OptionText });
            }
            return Ok(new { message = "MCQ updated" });
        }

        // ── DELETE /api/admin/mcq/{id} ────────────────────────────────────
        [HttpDelete("mcq/{id}")]
        public async Task<IActionResult> DeleteMcq(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                "DELETE FROM meaningoption_lang WHERE optionid IN (SELECT optionid FROM meaningoption WHERE questionid=@id)", new { id });
            await con.ExecuteAsync("DELETE FROM meaningoption WHERE questionid=@id", new { id });
            await con.ExecuteAsync("DELETE FROM meaningquestion_lang WHERE questionid=@id", new { id });
            await con.ExecuteAsync("DELETE FROM meaningquestion WHERE questionid=@id", new { id });
            return Ok(new { message = "MCQ deleted" });
        }

        // ── POST /api/admin/fillin ────────────────────────────────────────
        [HttpPost("fillin")]
        public async Task<IActionResult> AddFillIn([FromBody] FillInDto dto)
        {
            using var con = _db.CreateConnection();
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO fillinblank (lessonid, sentence_with_blank, correct_answer, option1, option2, option3, hint_ta, display_order)
                  VALUES (@lid, @sentence, @answer, @o1, @o2, @o3, @hint, @order) RETURNING id",
                new { lid = dto.LessonId, sentence = dto.SentenceWithBlank, answer = dto.CorrectAnswer,
                      o1 = dto.Option1, o2 = dto.Option2, o3 = dto.Option3, hint = dto.HintTa, order = dto.DisplayOrder });
            return Ok(new { id, message = "Fill-in-blank added" });
        }

        // ── PUT /api/admin/fillin/{id} ────────────────────────────────────
        [HttpPut("fillin/{id}")]
        public async Task<IActionResult> UpdateFillIn(int id, [FromBody] FillInDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE fillinblank SET sentence_with_blank=@sentence, correct_answer=@answer,
                  option1=@o1, option2=@o2, option3=@o3, hint_ta=@hint, display_order=@order WHERE id=@id",
                new { sentence = dto.SentenceWithBlank, answer = dto.CorrectAnswer, o1 = dto.Option1,
                      o2 = dto.Option2, o3 = dto.Option3, hint = dto.HintTa, order = dto.DisplayOrder, id });
            return Ok(new { message = "Updated" });
        }

        // ── DELETE /api/admin/fillin/{id} ─────────────────────────────────
        [HttpDelete("fillin/{id}")]
        public async Task<IActionResult> DeleteFillIn(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("DELETE FROM fillinblank WHERE id=@id", new { id });
            return Ok(new { message = "Deleted" });
        }

        // ── POST /api/admin/arrange ───────────────────────────────────────
        [HttpPost("arrange")]
        public async Task<IActionResult> AddArrange([FromBody] ArrangeDto dto)
        {
            using var con = _db.CreateConnection();
            var aid = await con.ExecuteScalarAsync<int>(
                "INSERT INTO arrangesentence (lessonid) VALUES (@lid) RETURNING arrangesentenceid",
                new { lid = dto.LessonId });
            await con.ExecuteAsync(
                "INSERT INTO arrangesentence_lang (arrangesentenceid, languageid, correctsentence, tamilmeaning) VALUES (@aid, 1, @s, @tm)",
                new { aid, s = dto.CorrectSentence, tm = string.IsNullOrWhiteSpace(dto.TamilMeaning) ? null : dto.TamilMeaning });
            var words = dto.CorrectSentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                var wid = await con.ExecuteScalarAsync<int>(
                    "INSERT INTO arrangesentenceword (arrangesentenceid, correctorder) VALUES (@aid, @ord) RETURNING wordid",
                    new { aid, ord = i + 1 });
                await con.ExecuteAsync(
                    "INSERT INTO arrangesentenceword_lang (wordid, languageid, wordtext) VALUES (@wid, 1, @w)",
                    new { wid, w = words[i] });
            }
            return Ok(new { id = aid, message = "Arrange sentence added" });
        }

        // ── DELETE /api/admin/arrange/{id} ────────────────────────────────
        [HttpDelete("arrange/{id}")]
        public async Task<IActionResult> DeleteArrange(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                "DELETE FROM arrangesentenceword_lang WHERE wordid IN (SELECT wordid FROM arrangesentenceword WHERE arrangesentenceid=@id)", new { id });
            await con.ExecuteAsync("DELETE FROM arrangesentenceword WHERE arrangesentenceid=@id", new { id });
            await con.ExecuteAsync("DELETE FROM arrangesentence_lang WHERE arrangesentenceid=@id", new { id });
            await con.ExecuteAsync("DELETE FROM arrangesentence WHERE arrangesentenceid=@id", new { id });
            return Ok(new { message = "Deleted" });
        }

        // ── PUT /api/admin/arrange/{id} ───────────────────────────────────
        // Edit the correct sentence + Tamil meaning (translation) and rebuild its word tiles.
        [HttpPut("arrange/{id}")]
        public async Task<IActionResult> UpdateArrange(int id, [FromBody] ArrangeDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE arrangesentence_lang SET correctsentence=@s, tamilmeaning=@tm
                  WHERE arrangesentenceid=@id AND languageid=1",
                new { s = dto.CorrectSentence, tm = string.IsNullOrWhiteSpace(dto.TamilMeaning) ? null : dto.TamilMeaning, id });

            // Rebuild the word tiles from the (possibly changed) sentence.
            await con.ExecuteAsync(
                "DELETE FROM arrangesentenceword_lang WHERE wordid IN (SELECT wordid FROM arrangesentenceword WHERE arrangesentenceid=@id)", new { id });
            await con.ExecuteAsync("DELETE FROM arrangesentenceword WHERE arrangesentenceid=@id", new { id });
            var words = dto.CorrectSentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                var wid = await con.ExecuteScalarAsync<int>(
                    "INSERT INTO arrangesentenceword (arrangesentenceid, correctorder) VALUES (@id, @ord) RETURNING wordid",
                    new { id, ord = i + 1 });
                await con.ExecuteAsync(
                    "INSERT INTO arrangesentenceword_lang (wordid, languageid, wordtext) VALUES (@wid, 1, @w)",
                    new { wid, w = words[i] });
            }
            return Ok(new { message = "Arrange/translate sentence updated" });
        }

        // ── Reading sentences ─────────────────────────────────────────────
        // GET list for a lesson (admin view)
        [HttpGet("reading/{lessonId}")]
        public async Task<IActionResult> GetReading(int lessonId)
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT readingsentenceid AS id, lessonid, sentencetext, displayorder
                  FROM readingsentence WHERE lessonid=@lid AND languageid=1
                  ORDER BY displayorder, readingsentenceid",
                new { lid = lessonId });
            return Ok(rows);
        }

        [HttpPost("reading")]
        public async Task<IActionResult> AddReading([FromBody] ReadingDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SentenceText)) return BadRequest(new { message = "Sentence text required" });
            using var con = _db.CreateConnection();
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO readingsentence (lessonid, sentencetext, languageid, displayorder)
                  VALUES (@lid, @st, 1, COALESCE((SELECT MAX(displayorder)+1 FROM readingsentence WHERE lessonid=@lid), @ord))
                  RETURNING readingsentenceid",
                new { lid = dto.LessonId, st = dto.SentenceText, ord = dto.DisplayOrder });
            return Ok(new { id, message = "Reading sentence added" });
        }

        [HttpPut("reading/{id}")]
        public async Task<IActionResult> UpdateReading(int id, [FromBody] ReadingDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                "UPDATE readingsentence SET sentencetext=@st, displayorder=@ord WHERE readingsentenceid=@id AND languageid=1",
                new { st = dto.SentenceText, ord = dto.DisplayOrder, id });
            return Ok(new { message = "Reading sentence updated" });
        }

        [HttpDelete("reading/{id}")]
        public async Task<IActionResult> DeleteReading(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("DELETE FROM readingsentence WHERE readingsentenceid=@id", new { id });
            return Ok(new { message = "Deleted" });
        }

        // ── GET /api/admin/export/reading ─────────────────────────────────
        [HttpGet("export/reading")]
        public async Task<IActionResult> ExportReading()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT r.readingsentenceid, r.lessonid, ll.lessonname, r.sentencetext, r.displayorder
                  FROM readingsentence r
                  JOIN lesson_lang ll ON ll.lessonid=r.lessonid AND ll.languageid=1
                  WHERE r.languageid=1
                  ORDER BY r.lessonid, r.displayorder");

            var csv = "ReadingSentenceId,LessonId,LessonName,SentenceText,DisplayOrder\n" +
                string.Join("\n", rows.Select(r =>
                    $"{r.readingsentenceid},{r.lessonid},\"{r.lessonname}\",\"{r.sentencetext}\",{r.displayorder}"));

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "reading_sentences.csv");
        }

        // ── POST /api/admin/import/reading ────────────────────────────────
        // CSV: LessonId, SentenceText, DisplayOrder(optional)
        [HttpPost("import/reading")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportReading(IFormFile file)
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
                    var text = cols[1].Trim();
                    if (string.IsNullOrWhiteSpace(text)) { skipped++; continue; }
                    int ord = cols.Length >= 3 && int.TryParse(cols[2], out int o) ? o : 0;

                    await con.ExecuteAsync(
                        @"INSERT INTO readingsentence (lessonid, sentencetext, languageid, displayorder)
                          VALUES (@lid, @st, 1, @ord)",
                        new { lid = lessonId, st = text, ord });
                    imported++;
                }
                catch { skipped++; }
            }
            return Ok(new { imported, skipped, message = $"Imported {imported} reading sentences, skipped {skipped}" });
        }

        // ── GET /api/admin/reports/overview ───────────────────────────────
        [HttpGet("reports/overview")]
        public async Task<IActionResult> GetReportsOverview()
        {
            using var con = _db.CreateConnection();
            var totalUsers = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE role != 'Admin'");
            var activeUsers = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE isactive=true AND role != 'Admin'");
            var totalLessons = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM lesson WHERE isactive=true");
            var completions = await con.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM user_lesson_progress WHERE is_completed=true");
            var topLesson = await con.QueryFirstOrDefaultAsync(
                @"SELECT ll.lessonname, COUNT(ulp.id) as completions
                  FROM user_lesson_progress ulp
                  JOIN lesson_lang ll ON ll.lessonid=ulp.lesson_id AND ll.languageid=1
                  WHERE ulp.is_completed=true
                  GROUP BY ll.lessonname ORDER BY completions DESC LIMIT 1");
            return Ok(new { totalUsers, activeUsers, totalLessons, completions, topLesson });
        }

        // ── GET /api/admin/reports/users ──────────────────────────────────
        [HttpGet("reports/users")]
        public async Task<IActionResult> GetUserReports()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT u.id, u.email, u.mobilenumber, u.role, u.isactive, u.createddate,
                         COUNT(DISTINCT ulp.lesson_id) FILTER (WHERE ulp.is_completed=true) as lessons_completed,
                         COALESCE(SUM(ulp.time_spent_seconds),0) as total_time_seconds,
                         COALESCE(SUM(ulp.correct_answers),0) as correct_answers,
                         COALESCE(SUM(ulp.wrong_answers),0) as wrong_answers,
                         MAX(ulp.last_activity) as last_activity
                  FROM users u
                  LEFT JOIN user_lesson_progress ulp ON ulp.user_id=u.id
                  WHERE u.role != 'Admin'
                  GROUP BY u.id ORDER BY u.id DESC LIMIT 500");
            return Ok(rows);
        }

        // ── GET /api/admin/reports/user/{userId} ──────────────────────────
        [HttpGet("reports/user/{userId}")]
        public async Task<IActionResult> GetUserReport(int userId)
        {
            using var con = _db.CreateConnection();
            var user = await con.QueryFirstOrDefaultAsync(
                "SELECT id, email, mobilenumber, role, isactive, createddate FROM users WHERE id=@id", new { id = userId });
            if (user == null) return NotFound();

            var lessonProgress = await con.QueryAsync(
                @"SELECT l.lessonid, ll.lessonname, l.lessonorder,
                         ulp.is_completed, ulp.completed_date, ulp.time_spent_seconds,
                         ulp.correct_answers, ulp.wrong_answers, ulp.total_attempts, ulp.last_activity
                  FROM lesson l
                  JOIN lesson_lang ll ON ll.lessonid=l.lessonid AND ll.languageid=1
                  LEFT JOIN user_lesson_progress ulp ON ulp.lesson_id=l.lessonid AND ulp.user_id=@uid
                  WHERE l.isactive=true ORDER BY l.lessonorder",
                new { uid = userId });

            var dailyActivity = await con.QueryAsync(
                @"SELECT DATE(last_activity) as activity_date,
                         COUNT(*) as lessons_touched,
                         SUM(time_spent_seconds) as time_spent
                  FROM user_lesson_progress
                  WHERE user_id=@uid AND last_activity IS NOT NULL
                  GROUP BY DATE(last_activity) ORDER BY activity_date DESC LIMIT 30",
                new { uid = userId });

            return Ok(new { user, lessonProgress, dailyActivity });
        }

        // ── POST /api/admin/progress/upsert ───────────────────────────────
        [HttpPost("progress/upsert")]
        public async Task<IActionResult> UpsertProgress([FromBody] UpsertProgressDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"INSERT INTO user_lesson_progress (user_id, lesson_id, is_completed, completed_date, time_spent_seconds, correct_answers, wrong_answers, total_attempts, last_activity)
                  VALUES (@uid, @lid, @done, @date, @time, @correct, @wrong, @attempts, NOW())
                  ON CONFLICT (user_id, lesson_id) DO UPDATE SET
                    is_completed = EXCLUDED.is_completed OR user_lesson_progress.is_completed,
                    completed_date = CASE WHEN EXCLUDED.is_completed THEN COALESCE(EXCLUDED.completed_date, NOW()) ELSE user_lesson_progress.completed_date END,
                    time_spent_seconds = user_lesson_progress.time_spent_seconds + EXCLUDED.time_spent_seconds,
                    correct_answers = user_lesson_progress.correct_answers + EXCLUDED.correct_answers,
                    wrong_answers = user_lesson_progress.wrong_answers + EXCLUDED.wrong_answers,
                    total_attempts = user_lesson_progress.total_attempts + EXCLUDED.total_attempts,
                    last_activity = NOW()",
                new { uid = dto.UserId, lid = dto.LessonId, done = dto.IsCompleted, date = dto.CompletedDate,
                      time = dto.TimeSpentSeconds, correct = dto.CorrectAnswers, wrong = dto.WrongAnswers,
                      attempts = dto.TotalAttempts });
            return Ok(new { message = "Progress saved" });
        }

        // ── GET /api/admin/lessons-list ────────────────────────────────────
        [HttpGet("lessons-list")]
        public async Task<IActionResult> GetLessonsList()
        {
            using var con = _db.CreateConnection();
            var lessons = await con.QueryAsync(
                @"SELECT l.lessonid, ll.lessonname, ll.description, l.lessonorder, l.isactive, l.is_premium, l.level,
                         COALESCE(l.category,'Grammar') AS category, l.package_id,
                         (SELECT COUNT(*) FROM lesson_word_content wc WHERE wc.lesson_id=l.lessonid) as word_count,
                         (SELECT COUNT(*) FROM meaningquestion mq WHERE mq.lessonid=l.lessonid) as mcq_count,
                         (SELECT COUNT(*) FROM fillinblank fb WHERE fb.lessonid=l.lessonid) as fillin_count,
                         (SELECT COUNT(*) FROM arrangesentence ar WHERE ar.lessonid=l.lessonid) as arrange_count,
                         (SELECT COUNT(*) FROM arrangesentence a JOIN arrangesentence_lang al ON al.arrangesentenceid=a.arrangesentenceid AND al.languageid=1 WHERE a.lessonid=l.lessonid AND al.tamilmeaning IS NOT NULL AND al.tamilmeaning != '') as translate_count
                  FROM lesson l
                  JOIN lesson_lang ll ON ll.lessonid=l.lessonid AND ll.languageid=1
                  ORDER BY l.lessonorder");
            return Ok(lessons);
        }

        // ── POST /api/admin/lessons ───────────────────────────────────────
        [HttpPost("lessons")]
        public async Task<IActionResult> CreateLesson([FromBody] CreateLessonDto dto)
        {
            using var con = _db.CreateConnection();
            var maxOrder = await con.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(lessonorder),0) FROM lesson") + 1;
            var level = string.IsNullOrWhiteSpace(dto.Level) ? "Beginner" : dto.Level;
            var allowedCats = new[] { "Grammar", "Vocabulary", "Conversation" };
            var category = allowedCats.Contains(dto.Category, StringComparer.OrdinalIgnoreCase) ? dto.Category : "Grammar";
            var lid = await con.ExecuteScalarAsync<int>(
                "INSERT INTO lesson (lessontypeid, lessonorder, isactive, is_premium, level, category, package_id) VALUES (1, @ord, true, @prem, @lvl, @cat, @pid) RETURNING lessonid",
                new { ord = dto.LessonOrder ?? maxOrder, prem = dto.IsPremium, lvl = level, cat = category, pid = dto.PackageId });
            await con.ExecuteAsync(
                "INSERT INTO lesson_lang (lessonid, languageid, lessonname, description) VALUES (@lid, 1, @name, @desc)",
                new { lid, name = dto.LessonName, desc = dto.Description });
            return Ok(new { id = lid, message = "Lesson created" });
        }

        // ── PUT /api/admin/lessons/{id} ────────────────────────────────────
        [HttpPut("lessons/{id}")]
        public async Task<IActionResult> UpdateLesson(int id, [FromBody] CreateLessonDto dto)
        {
            using var con = _db.CreateConnection();
            var lvl = string.IsNullOrWhiteSpace(dto.Level) ? "Beginner" : dto.Level;
            var allowedCats2 = new[] { "Grammar", "Vocabulary", "Conversation" };
            var cat = allowedCats2.Contains(dto.Category, StringComparer.OrdinalIgnoreCase) ? dto.Category : "Grammar";
            await con.ExecuteAsync(
                "UPDATE lesson SET lessonorder=@ord, isactive=@active, is_premium=@prem, level=@lvl, category=@cat, package_id=COALESCE(@pid, package_id) WHERE lessonid=@id",
                new { ord = dto.LessonOrder, active = dto.IsActive, prem = dto.IsPremium, lvl, cat, pid = dto.PackageId, id });
            await con.ExecuteAsync(
                "UPDATE lesson_lang SET lessonname=@name, description=@desc WHERE lessonid=@id AND languageid=1",
                new { name = dto.LessonName, desc = dto.Description, id });
            return Ok(new { message = "Lesson updated" });
        }

        // ── DELETE /api/admin/lessons/{id} ────────────────────────────────
        [HttpDelete("lessons/{id}")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("UPDATE lesson SET isactive=false WHERE lessonid=@id", new { id });
            return Ok(new { message = "Lesson deactivated" });
        }

        // ── POST /api/admin/users/{id}/lesson-access ──────────────────────
        [HttpPost("users/{id}/lesson-access")]
        public async Task<IActionResult> SetLessonAccess(int id, [FromBody] LessonAccessDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"INSERT INTO user_lesson_access (user_id, lesson_id, has_access)
                  VALUES (@uid, @lid, @access)
                  ON CONFLICT (user_id, lesson_id) DO UPDATE SET has_access=@access",
                new { uid = id, lid = dto.LessonId, access = dto.HasAccess });
            return Ok(new { message = "Access updated" });
        }

        // ── GET /api/admin/users/{id}/lesson-access ───────────────────────
        [HttpGet("users/{id}/lesson-access")]
        public async Task<IActionResult> GetUserLessonAccess(int id)
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT l.lessonid, ll.lessonname, l.lessonorder,
                         COALESCE(ula.has_access, true) as has_access
                  FROM lesson l
                  JOIN lesson_lang ll ON ll.lessonid=l.lessonid AND ll.languageid=1
                  LEFT JOIN user_lesson_access ula ON ula.lesson_id=l.lessonid AND ula.user_id=@uid
                  WHERE l.isactive=true ORDER BY l.lessonorder",
                new { uid = id });
            return Ok(rows);
        }
    }

    public record RoleDto(string Role);
    public record AdminResetPasswordDto(string NewPassword);
    public record PremiumDto(bool IsPremium);
    public record GrantAccessDto(bool Grant);
    public record CreateUserAdminDto(string Email, string Mobile, string Password, string? Role);
    public record UpdateUserAdminDto(string Email, string Mobile, string Role, bool IsActive, string? NewPassword);
    public record WordContentDto(int LessonId, string WordName, string SentencePattern, string DefinitionEn, string DefinitionTa, string ExampleEn, string ExampleTa, int DisplayOrder);
    public record McqOptionDto(string OptionText, bool IsCorrect);
    public record McqDto(int LessonId, string QuestionText, List<McqOptionDto> Options);
    public record FillInDto(int LessonId, string SentenceWithBlank, string CorrectAnswer, string? Option1, string? Option2, string? Option3, string? HintTa, int DisplayOrder);
    public record ArrangeDto(int LessonId, string CorrectSentence, string? TamilMeaning);
    public record ReadingDto(int LessonId, string SentenceText, int DisplayOrder);
    public record CreateLessonDto(string LessonName, string? Description, int? LessonOrder, bool IsActive, bool IsPremium, string? Level, string? Category, int? PackageId);
    public record LessonAccessDto(int LessonId, bool HasAccess);
    public record UpsertProgressDto(int UserId, int LessonId, bool IsCompleted, DateTime? CompletedDate, int TimeSpentSeconds, int CorrectAnswers, int WrongAnswers, int TotalAttempts);

    public record BulkWordRow(string WordName, string SentencePattern, string DefinitionEn, string DefinitionTa, string ExampleEn, string ExampleTa, int DisplayOrder);
    public record BulkWordContentDto(int LessonId, List<BulkWordRow> Rows);

    public record BulkMcqRow(string QuestionText, string Option1, string Option2, string Option3, string Option4, int CorrectOption);
    public record BulkMcqDto(int LessonId, List<BulkMcqRow> Questions);

    public record BulkFillInRow(string SentenceWithBlank, string CorrectAnswer, string Option1, string Option2, string Option3, string HintTa, int DisplayOrder);
    public record BulkFillInDto(int LessonId, List<BulkFillInRow> Rows);

    public record BulkArrangeDto(int LessonId, List<string> Sentences);
}

