using System.Net;
using System.Text.Json;

namespace SpokenEnglishAPI.Middleware
{
    public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "logs");

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await next(ctx);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
                WriteToFile(ctx, ex);
                await WriteError(ctx, ex);
            }
        }

        private static void WriteToFile(HttpContext ctx, Exception ex)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                var logFile = Path.Combine(LogDir, $"api_errors_{DateTime.UtcNow:yyyy-MM-dd}.log");
                var entry = $"[{DateTime.UtcNow:O}] {ctx.Request.Method} {ctx.Request.Path} | {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}---{Environment.NewLine}";
                File.AppendAllText(logFile, entry);
            }
            catch { /* never let logging crash the app */ }
        }

        private async Task WriteError(HttpContext ctx, Exception ex)
        {
            ctx.Response.ContentType = "application/json";
            var (status, code) = ex switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED"),
                KeyNotFoundException        => (HttpStatusCode.NotFound,     "NOT_FOUND"),
                ArgumentException           => (HttpStatusCode.BadRequest,   "BAD_REQUEST"),
                InvalidOperationException   => (HttpStatusCode.BadRequest,   "INVALID_OPERATION"),
                _                           => (HttpStatusCode.InternalServerError, "SERVER_ERROR")
            };
            ctx.Response.StatusCode = (int)status;

            var body = JsonSerializer.Serialize(new
            {
                error = code,
                message = ex.Message,
                traceId = ctx.TraceIdentifier
            });
            await ctx.Response.WriteAsync(body);
        }
    }
}
