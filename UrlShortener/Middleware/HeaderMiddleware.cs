
using System.Net;

namespace UrlShortener.Middleware;

public class HeaderMiddleware : IMiddleware
{
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
        }
        await next(context);
    }
}