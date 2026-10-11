


using System.Text.Json;
using UrlShortener.Interfaces;

namespace UrlShortener.Middleware;

public class AppRateLimiterMiddleware : IMiddleware
{
    private readonly IRateLimiter rateLimiter;

    public AppRateLimiterMiddleware(IRateLimiter rateLimiter)
    {
        this.rateLimiter = rateLimiter;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        if (!await rateLimiter.IsAllowed(partitionKey))
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsJsonAsync(
                JsonSerializer.Serialize(new { error = "Two many request" }));
            return;
        }
        await next(context);
    }
}