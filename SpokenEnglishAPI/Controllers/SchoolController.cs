using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchoolController : ControllerBase
    {
        private readonly DbContext _db;
        public SchoolController(DbContext db) { _db = db; }

        // ── Schools CRUD (Admin) ──────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSchools()
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync("SELECT * FROM sp_school_get_all()");
            return Ok(rows);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolDto dto)
        {
            using var con = _db.CreateConnection();
            var id = await con.ExecuteScalarAsync<int>(
                @"INSERT INTO schools (school_name, school_code, address, contact_email, contact_phone)
                  VALUES (@name, @code, @address, @email, @phone)
                  RETURNING school_id",
                new { name = dto.SchoolName, code = dto.SchoolCode, address = dto.Address,
                      email = dto.ContactEmail, phone = dto.ContactPhone });
            return Ok(new { schoolId = id, message = "School created" });
        }

        [HttpPut("{schoolId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSchool(int schoolId, [FromBody] CreateSchoolDto dto)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"UPDATE schools SET school_name=@name, school_code=@code, address=@address,
                  contact_email=@email, contact_phone=@phone WHERE school_id=@id",
                new { name = dto.SchoolName, code = dto.SchoolCode, address = dto.Address,
                      email = dto.ContactEmail, phone = dto.ContactPhone, id = schoolId });
            return Ok(new { message = "School updated" });
        }

        [HttpDelete("{schoolId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSchool(int schoolId)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync("UPDATE schools SET is_active=false WHERE school_id=@id", new { id = schoolId });
            return Ok(new { message = "School deactivated" });
        }

        // ── School Users ────────────────────────────────────────────

        [HttpGet("{schoolId}/users")]
        [Authorize]
        public async Task<IActionResult> GetSchoolUsers(int schoolId)
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                @"SELECT su.id, su.user_id, u.email, COALESCE(u.full_name, u.email) as full_name,
                         su.school_role, su.class_name, su.is_approved, su.created_date
                  FROM school_users su
                  JOIN users u ON u.id = su.user_id
                  WHERE su.school_id = @sid
                  ORDER BY su.school_role, u.email",
                new { sid = schoolId });
            return Ok(rows);
        }

        [HttpPost("{schoolId}/users/{userId}/approve")]
        [Authorize(Roles = "Admin,Headmaster")]
        public async Task<IActionResult> ApproveUser(int schoolId, int userId)
        {
            using var con = _db.CreateConnection();
            var approverId = GetCurrentUserId();
            await con.ExecuteAsync(
                @"UPDATE school_users SET is_approved=true, approved_by=@approver
                  WHERE school_id=@sid AND user_id=@uid",
                new { approver = approverId, sid = schoolId, uid = userId });
            return Ok(new { message = "User approved" });
        }

        [HttpPost("{schoolId}/teachers/{teacherId}/students/{studentId}")]
        [Authorize(Roles = "Admin,Headmaster")]
        public async Task<IActionResult> AssignStudentToTeacher(int schoolId, int teacherId, int studentId)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                @"INSERT INTO teacher_student_mapping (teacher_user_id, student_user_id, school_id)
                  VALUES (@tid, @sid, @schid) ON CONFLICT DO NOTHING",
                new { tid = teacherId, sid = studentId, schid = schoolId });
            return Ok(new { message = "Student assigned to teacher" });
        }

        [HttpDelete("{schoolId}/teachers/{teacherId}/students/{studentId}")]
        [Authorize(Roles = "Admin,Headmaster")]
        public async Task<IActionResult> RemoveStudentFromTeacher(int schoolId, int teacherId, int studentId)
        {
            using var con = _db.CreateConnection();
            await con.ExecuteAsync(
                "DELETE FROM teacher_student_mapping WHERE teacher_user_id=@tid AND student_user_id=@sid",
                new { tid = teacherId, sid = studentId });
            return Ok(new { message = "Assignment removed" });
        }

        // ── Reports ─────────────────────────────────────────────────

        [HttpGet("{schoolId}/progress")]
        [Authorize]
        public async Task<IActionResult> GetSchoolProgress(int schoolId, [FromQuery] int? teacherId)
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync(
                "SELECT * FROM sp_school_students_progress(@sid, @tid)",
                new { sid = schoolId, tid = (object?)teacherId ?? DBNull.Value });
            return Ok(rows);
        }

        [HttpGet("{schoolId}/stats")]
        [Authorize]
        public async Task<IActionResult> GetSchoolStats(int schoolId)
        {
            using var con = _db.CreateConnection();
            var stats = await con.QueryFirstOrDefaultAsync(
                @"SELECT
                    COUNT(*) FILTER (WHERE su.school_role = 'Student' AND su.is_approved) AS total_students,
                    COUNT(*) FILTER (WHERE su.school_role = 'Teacher' AND su.is_approved) AS total_teachers,
                    COUNT(*) FILTER (WHERE su.school_role = 'Headmaster') AS headmasters,
                    COUNT(*) FILTER (WHERE su.is_approved = false) AS pending_approvals,
                    COALESCE(AVG(sub.lessons_done), 0)::INT AS avg_lessons_per_student
                  FROM school_users su
                  LEFT JOIN (
                      SELECT ulp.user_id, COUNT(*) FILTER (WHERE ulp.is_completed) AS lessons_done
                      FROM user_lesson_progress ulp GROUP BY ulp.user_id
                  ) sub ON sub.user_id = su.user_id
                  WHERE su.school_id = @sid",
                new { sid = schoolId });
            return Ok(stats);
        }

        // ── Weekly Activity (Progress Dashboard) ──────────────────

        [HttpGet("weekly-activity/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetWeeklyActivity(int userId)
        {
            using var con = _db.CreateConnection();
            var rows = await con.QueryAsync("SELECT * FROM sp_user_weekly_activity(@uid)", new { uid = userId });
            return Ok(rows);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("id") ?? User.FindFirst("sub") ?? User.FindFirst("userId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    public record CreateSchoolDto(
        string SchoolName, string? SchoolCode, string? Address,
        string? ContactEmail, string? ContactPhone
    );
}
