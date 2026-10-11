
using StackExchange.Redis;
using UrlShortener.Interfaces;

namespace UrlShortener.RateLimiters;

public class FixedWindowLimiter : IRateLimiter
{
    private readonly FixedWindowRateLimiterOptions options;
    private readonly IConnectionMultiplexer redis;
    private readonly ILogger<FixedWindowLimiter> logger;
    private readonly IDatabase database;

    public FixedWindowLimiter(FixedWindowRateLimiterOptions options,
    IConnectionMultiplexer redis,
    ILogger<FixedWindowLimiter> logger)
    {
        this.options = options;
        this.redis = redis;
        this.logger = logger;
        this.database = redis.GetDatabase();
    }

    public async Task<bool> IsAllowed(string key)
    {
        var rateLimitKey = $"rate_limit:url:{key}";
        var counter = await database.StringIncrementAsync(rateLimitKey);

        if (counter == 1)
        {
            return await database.KeyExpireAsync(rateLimitKey, this.options.Window);
        }
        if (counter > this.options.PermitLimit)
        {
            return false;
        }
        return true;
    }
}

public class FixedWindowRateLimiterOptions
{
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    public int PermitLimit { get; set; } = 5;
}

