
using StackExchange.Redis;
using UrlShortener.Interfaces;

namespace UrlShortener.Services;

public class ApplicationConcurrencyPublisher : IConcurrencyPublisher
{
    private const string StreamName = "urlshortener:concurrency";
    private readonly ILogger<ApplicationConcurrencyPublisher> logger;
    private readonly IConnectionMultiplexer connectionMultiplexer;

    public ApplicationConcurrencyPublisher(ILogger<ApplicationConcurrencyPublisher> logger,
    IConnectionMultiplexer connectionMultiplexer)
    {
        this.logger = logger;
        this.connectionMultiplexer = connectionMultiplexer;
    }
    public async Task PublishAsync(EventType eventType, string requestId,
    CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = connectionMultiplexer.GetDatabase();

        await database.StreamAddAsync(StreamName, new NameValueEntry[]
        {
            new(nameof(eventType), eventType.ToString()),
                new("requestId", requestId),
                new("occurredAt", DateTimeOffset.UtcNow.ToString("O"))
        });

        // logger.LogInformation(
        //     "Published concurrency event for {ShortCode}",
        //     shortCode);
    }
}