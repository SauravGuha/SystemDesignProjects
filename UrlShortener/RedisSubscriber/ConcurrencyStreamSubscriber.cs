
using StackExchange.Redis;

public class ConcurrencyStreamSubscriber : BackgroundService
{
    private const string StreamName = "urlshortener:concurrency";
    private const string GroupName = "urlshortener-workers";
    private readonly IConnectionMultiplexer redis;
    private readonly ILogger<ConcurrencyStreamSubscriber> logger;
    private readonly string consumerName;

    public ConcurrencyStreamSubscriber(IConnectionMultiplexer redis,
    ILogger<ConcurrencyStreamSubscriber> logger)
    {
        this.redis = redis;
        this.logger = logger;
        this.consumerName = $"{Environment.MachineName}_{nameof(ConcurrencyStreamSubscriber)}";
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var database = redis.GetDatabase();
        while (!stoppingToken.IsCancellationRequested)
        {
            await this.CreateConsumerGroup(database);

            // Read new messages for the consumer
            var entries = database.StreamReadGroup(StreamName, GroupName,
            consumerName, ">", count: 10);

            if (entries.Count() > 0)
            {
                foreach (var entry in entries)
                {
                    try
                    {
                        this.logger.LogInformation("Message entry id: {0}", entry.Id);
                        database.StreamAcknowledge(StreamName, GroupName, entry.Id);
                    }
                    catch
                    {

                    }
                }
            }

            await Task.Delay(5000);
        }
    }

    private Task CreateConsumerGroup(IDatabase database)
    {
        try
        {
            database.StreamCreateConsumerGroup(StreamName, GroupName,
            StreamPosition.Beginning, createStream: true);
        }
        catch (RedisServerException exception)
            when (exception.Message.Contains("BUSYGROUP"))
        {
        }

        return Task.CompletedTask;
    }
}