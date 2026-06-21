using System.Net;
using System.Text.Json;

namespace SpokenEnglishAPI.Middleware
{
    public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await next(ctx);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
                await WriteError(ctx, ex);
            }
        }

        private static async Task WriteError(HttpContext ctx, Exception ex)
        {
            ctx.Response.ContentType = "application/json";
            var (status, code) = ex switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, "FORBIDDEN"),
                KeyNotFoundException        => (HttpStatusCode.NotFound,  "NOT_FOUND"),
                ArgumentException           => (HttpStatusCode.BadRequest, "BAD_REQUEST"),
                InvalidOperationException   => (HttpStatusCode.BadRequest, "INVALID_OPERATION"),
                _                           => (HttpStatusCode.InternalServerError, "SERVER_ERROR")
            };
            ctx.Response.StatusCode = (int)status;

            var body = JsonSerializer.Serialize(new
            {
                error = code,
                message = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                    ? ex.Message
                    : "An unexpected error occurred. Please try again.",
                traceId = ctx.TraceIdentifier
            });
            await ctx.Response.WriteAsync(body);
        }
    }
}
