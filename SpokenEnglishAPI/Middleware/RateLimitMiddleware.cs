using System.Collections.Concurrent;
using NLog;

namespace SpokenEnglishAPI.Middleware
{
    /// <summary>
    /// Stricter brute-force protection for authentication endpoints.
    /// The global rate limiter (100 req/min) is fine for normal traffic but far too
    /// loose for login/password endpoints, where an attacker could try ~100 passwords
    /// a minute per IP. This middleware caps sensitive auth paths to a small number of
    /// attempts per rolling window, per client IP.
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        // Paths that get the stricter limit (case-insensitive, prefix match).
        private static readonly string[] _protectedPaths =
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/reset-password",
            "/api/auth/otp-login",
        };

        private const int MaxAttempts = 10;                 // attempts allowed...
        private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);   // ...per window, per IP

        // ip -> (windowStart, count). Pruned lazily on access.
        private static readonly ConcurrentDictionary<string, (DateTime windowStart, int count)> _hits = new();

        public RateLimitMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            var isProtected = _protectedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (!isProtected)
            {
                await _next(context);
                return;
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;

            var entry = _hits.AddOrUpdate(
                ip,
                _ => (now, 1),
                (_, cur) => (now - cur.windowStart > Window) ? (now, 1) : (cur.windowStart, cur.count + 1));

            if (entry.count > MaxAttempts)
            {
                _logger.Warn("Auth rate limit exceeded for IP {0} on {1} ({2} attempts)", ip, path, entry.count);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = ((int)Window.TotalSeconds).ToString();
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "AUTH_RATE_LIMIT_EXCEEDED",
                    message = "Too many attempts. Please wait a few minutes and try again."
                });
                return;
            }

            await _next(context);
        }
    }
}
