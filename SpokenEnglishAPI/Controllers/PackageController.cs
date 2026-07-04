using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Infrastructure.Data;
using SpokenEnglishAPI.Infrastructure.Security;

namespace SpokenEnglishAPI.Controllers
{
    /// <summary>
    /// Learning Packages (Beginner / Intermediate / Advanced). Each package groups
    /// lessons by category (Grammar / Vocabulary / Conversation). Additive feature —
    /// existing lesson/flow endpoints are unchanged.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PackageController : ControllerBase
    {
        private readonly DbContext _db;
        public PackageController(DbContext db) => _db = db;

        // Map a user's fine-grained level to one of the 3 package levels.
        public static string LevelToPackageLevel(string? level) => (level ?? "Beginner") switch
        {
            "Beginner" or "Elementary" => "Beginner",
            "Intermediate" => "Intermediate",
            "Advanced" or "College" or "Professional" => "Advanced",
            _ => "Beginner",
        };

        // ── GET /api/package ──────────────────────────────────────────────
        // Public list of active packages (used on register/dashboard).
        [HttpGet]
        public async Task<IActionResult> GetPackages()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT p.package_id, p.name, p.level, p.description, p.display_order, p.is_active,
                         (SELECT COUNT(*) FROM lesson l WHERE l.package_id = p.package_id AND l.isactive = true) AS lesson_count
                  FROM learning_package p
                  WHERE p.is_active = true
                  ORDER BY p.display_order, p.package_id");
            return Ok(rows);
        }

        // ── GET /api/package/{id} ─────────────────────────────────────────
        // Package detail with its lessons grouped by category.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackage(int id)
        {
            using var con = _db.CreateConnection();
            var pkg = await con.QueryFirstOrDefaultAsync(
                "SELECT package_id, name, level, description, display_order, is_active FROM learning_package WHERE package_id=@id",
                new { id });
            if (pkg == null) return NotFound(new { message = "Package not found" });

            var lessons = await con.QueryAsync(
                @"SELECT l.lessonid, ll.lessonname, ll.description, l.lessonorder,
                         COALESCE(l.category,'Grammar') AS category, l.level, l.is_premium, l.isactive
                  FROM lesson l
                  JOIN lesson_lang ll ON ll.lessonid = l.lessonid AND ll.languageid = 1
                  WHERE l.package_id = @id
                  ORDER BY l.lessonorder",
                new { id });

            return Ok(new { package = pkg, lessons });
        }

        // ── GET /api/package/{id}/progress/{userId} ───────────────────────
        // Per-category + overall completion % for a user within a package.
        [HttpGet("{id}/progress/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetPackageProgress(int id, int userId)
        {
            if (!OwnershipGuard.CanAccess(User, userId)) return Forbid();
            using var con = _db.CreateConnection();

            var byCategory = await con.QueryAsync(
                @"SELECT COALESCE(l.category,'Grammar') AS category,
                         COUNT(*) AS total,
                         COUNT(*) FILTER (WHERE ulp.is_completed = true) AS completed
                  FROM lesson l
                  LEFT JOIN user_lesson_progress ulp
                         ON ulp.lesson_id = l.lessonid AND ulp.user_id = @uid
                  WHERE l.package_id = @id AND l.isactive = true
                  GROUP BY COALESCE(l.category,'Grammar')
                  ORDER BY category",
                new { id, uid = userId });

            var overall = await con.QueryFirstOrDefaultAsync(
                @"SELECT COUNT(*) AS total,
                         COUNT(*) FILTER (WHERE ulp.is_completed = true) AS completed
                  FROM lesson l
                  LEFT JOIN user_lesson_progress ulp
                         ON ulp.lesson_id = l.lessonid AND ulp.user_id = @uid
                  WHERE l.package_id = @id AND l.isactive = true",
                new { id, uid = userId });

            int total = (int)(overall?.total ?? 0L);
            int completed = (int)(overall?.completed ?? 0L);
            int percent = total > 0 ? (int)Math.Round(completed * 100.0 / total) : 0;

            return Ok(new { packageId = id, total, completed, percent, byCategory });
        }

        // ── POST /api/package (Admin) ─────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] PackageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { message = "Name required" });
            using var con = _db.CreateConnection();
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO learning_package (name, level, description, display_order, is_active)
                  VALUES (@name, @level, @desc, @ord, true) RETURNING package_id",
                new { name = dto.Name, level = string.IsNullOrWhiteSpace(dto.Level) ? "Beginner" : dto.Level,
                      desc = dto.Description, ord = dto.DisplayOrder });
            return Ok(new { id, message = "Package created" });
        }

        // ── PUT /api/package/{id} (Admin) ─────────────────────────────────
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] PackageDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE learning_package
                  SET name=@name, level=@level, description=@desc, display_order=@ord, is_active=@active
                  WHERE package_id=@id",
                new { name = dto.Name, level = string.IsNullOrWhiteSpace(dto.Level) ? "Beginner" : dto.Level,
                      desc = dto.Description, ord = dto.DisplayOrder, active = dto.IsActive, id });
            return Ok(new { message = "Package updated" });
        }

        // ── DELETE /api/package/{id} (Admin) ──────────────────────────────
        // Soft-delete: deactivate and unlink lessons (lessons themselves are kept).
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("UPDATE lesson SET package_id = NULL WHERE package_id=@id", new { id });
            await con.ExecuteAsync("UPDATE learning_package SET is_active=false WHERE package_id=@id", new { id });
            return Ok(new { message = "Package deactivated" });
        }

        // ── POST /api/package/assign-lesson (Admin) ───────────────────────
        // Assign a lesson to a package and set its category.
        [HttpPost("assign-lesson")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignLesson([FromBody] AssignLessonDto dto)
        {
            var allowed = new[] { "Grammar", "Vocabulary", "Conversation" };
            var category = allowed.Contains(dto.Category, StringComparer.OrdinalIgnoreCase) ? dto.Category : "Grammar";
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                "UPDATE lesson SET package_id=@pid, category=@cat WHERE lessonid=@lid",
                new { pid = dto.PackageId, cat = category, lid = dto.LessonId });
            return Ok(new { message = "Lesson assigned to package" });
        }
    }

    public record PackageDto(string Name, string? Level, string? Description, int DisplayOrder, bool IsActive);
    public record AssignLessonDto(int LessonId, int? PackageId, string Category);
}
