using Dapper;
using NLog;
using SpokenEnglishAPI.Application.Interfaces;
using SpokenEnglishAPI.Infrastructure.Data;

namespace SpokenEnglishAPI.Application.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly DbContext _db;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public AuditLogService(DbContext db) => _db = db;

        public async Task LogAsync(string action, int? userId, string? email, string? ipAddress,
            string? userAgent, bool success, string? detail = null)
        {
            try
            {
                using var con = _db.CreateConnection();
                await con.ExecuteAsync(
                    @"INSERT INTO audit_log (user_id, email, action, ip_address, user_agent, success, detail, created_at)
                      VALUES (@userId, @email, @action, @ip, @ua, @success, @detail, NOW())",
                    new
                    {
                        userId,
                        email = Trunc(email, 200),
                        action = Trunc(action, 100),
                        ip = Trunc(ipAddress, 64),
                        ua = Trunc(userAgent, 400),
                        success,
                        detail = Trunc(detail, 1000)
                    });
            }
            catch (Exception ex)
            {
                // Never let an audit-log failure break the caller's request.
                _logger.Warn(ex, "Failed to write audit_log entry for action {0}", action);
            }
        }

        private static string? Trunc(string? s, int max)
            => string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max));
    }
}
