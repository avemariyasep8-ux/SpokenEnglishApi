namespace SpokenEnglishAPI.Middleware
{
    public class SecurityHeadersMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext ctx)
        {
            var h = ctx.Response.Headers;

            // Prevent MIME sniffing
            h["X-Content-Type-Options"] = "nosniff";

            // Block framing (clickjacking)
            h["X-Frame-Options"] = "DENY";

            // Legacy XSS filter (IE/old Chrome)
            h["X-XSS-Protection"] = "1; mode=block";

            // Referrer leakage
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Restrict feature access
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // HSTS — 1 year, include subdomains (Railway serves HTTPS)
            h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            // CSP: API only serves JSON, no HTML/scripts needed
            h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            // Remove server fingerprint header
            h.Remove("Server");
            h.Remove("X-Powered-By");

            await next(ctx);
        }
    }
}
