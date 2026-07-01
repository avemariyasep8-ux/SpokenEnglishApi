namespace SpokenEnglishAPI.Application.Interfaces
{
    /// <summary>
    /// Records security-relevant user actions (login, logout, register, admin changes)
    /// to the audit_log table. All methods are fire-and-forget safe — a logging
    /// failure must never break the request that triggered it.
    /// </summary>
    public interface IAuditLogService
    {
        Task LogAsync(string action, int? userId, string? email, string? ipAddress,
            string? userAgent, bool success, string? detail = null);
    }
}
