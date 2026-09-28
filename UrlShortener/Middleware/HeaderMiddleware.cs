
using System.Net;
using UrlShortener.Interfaces;
using UrlShortener.Models;

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
                var redisKey = await applicationCache.GetValueAsync<string>(iValue!, CancellationToken.None);
                if (redisKey == null)
                {
                    var cacheModel = new CacheModels<string>
                    {
                        Value = "",
                        RequestStatus = RequestStatus.Pending
                    };
                    await applicationCache.SetValueAsync(iValue!, cacheModel, CancellationToken.None);
                }
                context.Items.Add("Idempotent-Key", iValue);
            }
        }
        await next(context);
    }
}