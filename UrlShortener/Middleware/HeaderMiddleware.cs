
using System.Net;
using UrlShortener.Interfaces;

namespace UrlShortener.Middleware;

public class HeaderMiddleware : IMiddleware
{
    private readonly IApplicationCache applicationCache;

    public HeaderMiddleware(IApplicationCache applicationCache)
    {
        this.applicationCache = applicationCache;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var method = context.Request.Method.ToUpper();
        if (method == "POST" || method == "PUT")
        {
            var iValue = context.Request.Headers["Idempotent-Key"];
            if (string.IsNullOrWhiteSpace(iValue))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "text/xml";
                await context.Response.WriteAsync("Idempotent-Key for post, patch is missing");
                return;
            }
            else
            {
                context.Items.Add("Idempotent-Key", iValue);
                await applicationCache.SetValueAsync(iValue!, true.ToString(), CancellationToken.None);
            }
        }
        await next(context);
    }
}