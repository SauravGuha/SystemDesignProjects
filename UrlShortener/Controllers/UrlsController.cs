
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Data;
using UrlShortener.Data.Models;
using UrlShortener.Dtos;
using UrlShortener.Interfaces;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class UrlsController : ControllerBase
{
    private readonly ILogger<UrlsController> logger;
    private readonly ShortUrlDbContext shortUrlDbContext;
    private readonly IShortCodeGenerator shortCodeGenerator;
    private readonly IApplicationCache applicationCache;

    public UrlsController(ILogger<UrlsController> logger,
    ShortUrlDbContext shortUrlDbContext, IShortCodeGenerator shortCodeGenerator,
    IApplicationCache applicationCache)
    {
        this.logger = logger;
        this.shortUrlDbContext = shortUrlDbContext;
        this.shortCodeGenerator = shortCodeGenerator;
        this.applicationCache = applicationCache;
    }

    [HttpPost]
    public async Task<IActionResult> ShortenUrl(
        [FromBody] UrlsRequest data,
        CancellationToken cancellationToken)
    {
        var idempotentKey =
            HttpContext.Items["Idempotent-Key"]!.ToString()!;

        var result = await applicationCache
            .TryAcquireIdempotencyAsync(
                idempotentKey,
                cancellationToken);

        if (result == "IN_PROGRESS")
        {
            return Conflict(
                $"{idempotentKey} is already under process");
        }

        if (result == "COMPLETED")
        {
            var completed =
                await applicationCache.GetValueAsync<string>(
                    idempotentKey,
                    cancellationToken);

            return Ok(new
            {
                shortUrl = completed!.Value
            });
        }

        if (result == "ACQUIRED")
        {
            var hash =
                shortCodeGenerator.GenerateShortCode(data.Url);

            var shortUrl = new ShortUrl(hash, data.Url);

            shortUrlDbContext.ShortUrls.Add(shortUrl);

            await shortUrlDbContext.SaveChangesAsync(
                cancellationToken);

            var completedValue =
                new CacheModels<string>
                {
                    RequestStatus = RequestStatus.Completed,
                    Value = hash
                };

            await applicationCache.SetValueAsync(
                idempotentKey,
                completedValue,
                cancellationToken);

            return Ok(new
            {
                shortUrl = hash
            });
        }

        return BadRequest("Unable to process idempotency key");
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> GetActualUrl(string shortCode, CancellationToken cancellationToken)
    {
        var cacheValue = await this.applicationCache.GetValueAsync<string>(shortCode, cancellationToken);
        if (cacheValue == null)
        {
            var lockValue = await this.applicationCache.AcquireLockAsync(shortCode,
            TimeSpan.FromSeconds(5), cancellationToken);
            if (lockValue != null)
            {
                try
                {
                    cacheValue = await this.applicationCache.GetValueAsync<string>(shortCode, cancellationToken);
                    if (cacheValue == null)
                    {
                        //obtain the actual url
                        var urlData = this.shortUrlDbContext.ShortUrls.
                        FirstOrDefault(e => e.ShortCode == shortCode);
                        //every time database gets hit, record the counter
                        if (urlData != null)
                        {
                            urlData.UpdatehitCount();
                            await this.shortUrlDbContext.SaveChangesAsync(cancellationToken);
                            await this.applicationCache.SetValueAsync(shortCode,
                            new CacheModels<string>
                            {
                                Value = urlData.LongUrl
                            }, cancellationToken);
                            return Ok(new { longurl = urlData.LongUrl });
                        }
                        else
                            return NotFound($"{shortCode} not found");
                    }
                    else
                        return Ok(new { longurl = cacheValue.Value });
                }
                finally
                {
                    await this.applicationCache.ReleaseLockAsync(shortCode, lockValue, cancellationToken);
                }

            }
            else
                return StatusCode(408);
        }
        else
            return Ok(new { longurl = cacheValue.Value });

    }

    [HttpGet("{shortCode}/stats")]
    public async Task<IActionResult> UrlStats(string shortCode)
    {
        //obtain the url stats
        return Ok("");
    }

}