
using StackExchange.Redis;
using UrlShortener.Interfaces;

namespace UrlShortener.Services;

/// <summary>
/// covers everything you need for distributed caching: key-value storage, expiration, persistence, replication, and pub/sub.
/// </summary>
public class ApplicationRedisCache : IApplicationCache
{
    private string? connectionString;
    private ConnectionMultiplexer connectionMultiplexer;

    public ApplicationRedisCache(IConfiguration configuration)
    {
        this.connectionString = configuration.GetConnectionString("redissocket");
        if (string.IsNullOrWhiteSpace(this.connectionString))
            throw new ArgumentNullException("redissocket connection not found");
        this.connectionMultiplexer = ConnectionMultiplexer.Connect(this.connectionString);
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        var database = connectionMultiplexer.GetDatabase();
        var batch = database.CreateBatch();
        var getTask = batch.StringGetAsync(key);
        var expireTask = batch.KeyExpireAsync(key, TimeSpan.FromMinutes(5));
        batch.Execute();
        await Task.WhenAll(getTask, expireTask);

        var value = await getTask;
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        var database = connectionMultiplexer.GetDatabase();
        await database.StringSetAsync(key, value, TimeSpan.FromMinutes(10));
    }

    public async Task<Boolean> DeleteKeyAsync(string key, CancellationToken token)
    {
        var database = connectionMultiplexer.GetDatabase();
        bool _isKeyExist = await database.KeyExistsAsync(key);
        if (_isKeyExist == true)
        {
            return await database.KeyDeleteAsync(key);
        }
        return false;
    }
}