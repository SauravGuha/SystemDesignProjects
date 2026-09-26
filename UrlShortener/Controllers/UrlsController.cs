
using Microsoft.AspNetCore.Mvc;
using UrlShortener.Data;
using UrlShortener.Data.Models;
using UrlShortener.Dtos;
using UrlShortener.Interfaces;

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
    ShortUrlDbContext shortUrlDbContext, IShortCodeGenerator shortCodeGenerator, IApplicationCache applicationCache)
    {
        this.logger = logger;
        this.shortUrlDbContext = shortUrlDbContext;
        this.shortCodeGenerator = shortCodeGenerator;
        this.applicationCache = applicationCache;
    }

    [HttpPost]
    public async Task<IActionResult> ShortenUrl([FromBody] UrlsRequest data, CancellationToken cancellationToken)
    {
        var idempotentKey = this.HttpContext.Items["Idempotent-Key"]!.ToString();
        var processing = await this.applicationCache.GetValueAsync(idempotentKey!, cancellationToken);
        if (string.IsNullOrWhiteSpace(processing))
        {
            var hash = shortCodeGenerator.GenerateShortCode(data.Url);
            var shortUrl = new ShortUrl(hash, data.Url);
            this.shortUrlDbContext.ShortUrls.Add(shortUrl);
            await Task.Delay(10000);
            await this.shortUrlDbContext.SaveChangesAsync(cancellationToken);
            await this.applicationCache.DeleteKeyAsync(idempotentKey!, cancellationToken);
            return Ok(new { shortUrl = hash });
        }
        else
        {
            return Conflict($"{idempotentKey} is already under process");
        }
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> GetActualUrl(string shortCode, CancellationToken cancellationToken)
    {
        //obtain the actual url
        var urlData = this.shortUrlDbContext.ShortUrls.FirstOrDefault(e => e.ShortCode == shortCode);
        if (urlData != null)
            return Redirect(urlData.LongUrl);
        else
            return NotFound($"{shortCode} not found");
    }

    [HttpGet("{shortCode}/stats")]
    public async Task<IActionResult> UrlStats(string shortCode)
    {
        //obtain the url stats
        return Ok("");
    }
}