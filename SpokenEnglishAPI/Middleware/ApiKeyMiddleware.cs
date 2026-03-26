using SpokenEnglishAPI.Infrastructure.Repositories;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, UserRepository repo)
    {
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var apiKey)
            || !repo.IsValidApiKey(apiKey))
        {
            context.Response.StatusCode = 403;
            return;
        }

        await _next(context);
    }
}
