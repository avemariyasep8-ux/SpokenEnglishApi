using System.Net;
using System.Text.Json;
using NLog;
using SpokenEnglishAPI.Application.Services;

namespace SpokenEnglishAPI.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private static readonly Logger _nlog = LogManager.GetCurrentClassLogger();

        private readonly RequestDelegate _next;
        private readonly IEmailAlertService _email;

        public GlobalExceptionMiddleware(RequestDelegate next, IEmailAlertService email)
        {
            _next  = next;
            _email = email;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                await HandleAsync(ctx, ex);
            }
        }

        private async Task HandleAsync(HttpContext ctx, Exception ex)
        {
            var method  = ctx.Request.Method;
            var path    = ctx.Request.Path;
            var traceId = ctx.TraceIdentifier;

            // NLog structured log
            _nlog.Error(ex, "Unhandled exception | TraceId={TraceId} | {Method} {Path}", traceId, method, path);

            // Send email alert asynchronously (fire-and-forget so it never blocks the response)
            _ = Task.Run(async () =>
            {
                var subject = $"{ex.GetType().Name} on {method} {path}";
                var body    = $"""
                    TraceId  : {traceId}
                    Time     : {DateTime.UtcNow:O}
                    Request  : {method} {path}
                    Exception: {ex.GetType().FullName}
                    Message  : {ex.Message}

                    Stack Trace:
                    {ex.StackTrace}

                    Inner Exception:
                    {ex.InnerException?.ToString() ?? "(none)"}
                    """;
                await _email.SendExceptionAlertAsync(subject, body);
            });

            ctx.Response.ContentType = "application/json";
            var (status, code) = ex switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized,     "UNAUTHORIZED"),
                KeyNotFoundException        => (HttpStatusCode.NotFound,         "NOT_FOUND"),
                ArgumentException           => (HttpStatusCode.BadRequest,       "BAD_REQUEST"),
                InvalidOperationException   => (HttpStatusCode.BadRequest,       "INVALID_OPERATION"),
                _                           => (HttpStatusCode.InternalServerError, "SERVER_ERROR"),
            };
            ctx.Response.StatusCode = (int)status;

            var body2 = JsonSerializer.Serialize(new
            {
                error   = code,
                message = ex.Message,
                traceId,
            });
            await ctx.Response.WriteAsync(body2);
        }
    }
}
