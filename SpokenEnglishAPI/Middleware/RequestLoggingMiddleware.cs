using System.Diagnostics;

namespace SpokenEnglishAPI.Middleware
{
    public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext ctx)
        {
            var sw = Stopwatch.StartNew();
            await next(ctx);
            sw.Stop();

            var level = ctx.Response.StatusCode >= 500 ? LogLevel.Error
                      : ctx.Response.StatusCode >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            logger.Log(level,
                "{Method} {Path}{Query} → {Status} in {Ms}ms | IP:{IP}",
                ctx.Request.Method,
                ctx.Request.Path,
                ctx.Request.QueryString,
                ctx.Response.StatusCode,
                sw.ElapsedMilliseconds,
                ctx.Connection.RemoteIpAddress);
        }
    }
}
