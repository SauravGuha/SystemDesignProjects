
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

    public UrlsController(ILogger<UrlsController> logger,
    ShortUrlDbContext shortUrlDbContext, IShortCodeGenerator shortCodeGenerator)
    {
        this.logger = logger;
        this.shortUrlDbContext = shortUrlDbContext;
        this.shortCodeGenerator = shortCodeGenerator;
    }

    [HttpPost]
    public async Task<IActionResult> ShortenUrl([FromBody] UrlsRequest data, CancellationToken cancellationToken)
    {
        var hash = shortCodeGenerator.GenerateShortCode(data.Url);
        var shortUrl = new ShortUrl(hash, data.Url);
        this.shortUrlDbContext.ShortUrls.Add(shortUrl);
        await this.shortUrlDbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { shortUrl = hash });
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> GetActualUrl(string shortCode)
    {
        //obtain the actual url
        return Redirect("actual url");
    }

    [HttpGet("{shortCode}/stats")]
    public async Task<IActionResult> UrlStats(string shortUrl)
    {
        //obtain the url stats
        return Ok("");
    }
}