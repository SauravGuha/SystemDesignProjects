
using Microsoft.EntityFrameworkCore;
using UrlShortener.Interfaces;

namespace UrlShortener.Middleware;

public class ExceptionMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionMiddleware> logger;
    private readonly IConcurrencyPublisher concurrencyPublisher;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger,
    IConcurrencyPublisher concurrencyPublisher)
    {
        this.logger = logger;
        this.concurrencyPublisher = concurrencyPublisher;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (DbUpdateConcurrencyException dex)
        {
            this.logger.LogError(exception: dex, message: "Publishing to queue");
            try
            {
                await this.concurrencyPublisher.PublishAsync(EventType.DbUpdateConcurrencyException,
                context.TraceIdentifier, CancellationToken.None);
            }
            catch (Exception publishException)
            {
                logger.LogError(
                    publishException,
                    "Failed to publish concurrency event");
            }

            context.Response.StatusCode =
                StatusCodes.Status409Conflict;

            await context.Response.WriteAsJsonAsync(new
            {
                error = "The URL was updated by another request."
            });
        }
    }
}