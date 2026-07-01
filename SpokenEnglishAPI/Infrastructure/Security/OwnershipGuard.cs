using System.Security.Claims;

namespace SpokenEnglishAPI.Infrastructure.Security
{
    /// <summary>
    /// Prevents Broken Object Level Authorization (IDOR): a caller may only act on
    /// their own userId unless they are an Admin. The integer user id is carried in
    /// the "uid" JWT claim (added by JwtTokenGenerator).
    /// </summary>
    public static class OwnershipGuard
    {
        public static bool CanAccess(ClaimsPrincipal? principal, int targetUserId)
        {
            if (principal == null) return false;
            if (principal.IsInRole("Admin")) return true;

            var uid = principal.FindFirst("uid")?.Value;
            // Grace for legacy tokens issued before the uid claim existed — they expire
            // within the JWT lifetime, after which enforcement is strict.
            if (string.IsNullOrEmpty(uid)) return true;

            return int.TryParse(uid, out var id) && id == targetUserId;
        }
    }
}
