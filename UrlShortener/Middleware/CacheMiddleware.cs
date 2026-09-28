
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Interfaces;

namespace UrlShortener.Middleware;

public class CacheMiddleware : IMiddleware
{
    private readonly IApplicationCache applicationCache;

    public CacheMiddleware(IApplicationCache applicationCache)
    {
        this.applicationCache = applicationCache;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var method = context.Request.Method.ToUpper();
        var path = context.Request.Path.Value;
        if (method == "GET")
        {
            var requestKey = $"{method}_{path}";
            var redisKey = await applicationCache.GetValueAsync<string>(requestKey, CancellationToken.None);
            if (redisKey != null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(redisKey.Value);
            }
            else
            {
                var orginalBody = context.Response.Body;
                var ms = new MemoryStream();
                context.Response.Body = ms;
                await next(context);
                ms.Seek(0, SeekOrigin.Begin);
                var reader = new StreamReader(ms);
                var data = reader.ReadToEnd();
                await applicationCache.SetValueAsync<string>(requestKey, new Models.CacheModels<string>
                {
                    RequestStatus = Models.RequestStatus.Completed,
                    Value = data
                }, CancellationToken.None);
                ms.Seek(0, SeekOrigin.Begin);
                await ms.CopyToAsync(orginalBody);
                context.Response.Body = orginalBody;
            }
        }
    }
}