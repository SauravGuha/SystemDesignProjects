
using System.Text.Json;
using StackExchange.Redis;
using UrlShortener.Interfaces;
using UrlShortener.Models;

namespace UrlShortener.Services;

/// <summary>
/// covers everything you need for distributed caching: key-value storage, expiration, persistence, replication, and pub/sub.
/// </summary>
public class ApplicationRedisCache : IApplicationCache
{
    private IConnectionMultiplexer connectionMultiplexer;

    public ApplicationRedisCache(IConnectionMultiplexer connectionMultiplexer)
    {
        this.connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<CacheModels<T>?> GetValueAsync<T>(string key, CancellationToken cancellationToken)
    {
        var database = connectionMultiplexer.GetDatabase();
        var batch = database.CreateBatch();
        var getTask = batch.StringGetAsync(key);
        var expireTask = batch.KeyExpireAsync(key, TimeSpan.FromMinutes(5));
        batch.Execute();
        await Task.WhenAll(getTask, expireTask);

        var value = await getTask;
        return value.HasValue
        ? JsonSerializer.Deserialize<CacheModels<T>>(value.ToString())
        : null;
    }

    public async Task SetValueAsync<T>(string key, CacheModels<T> value, CancellationToken cancellationToken)
    {
        var database = connectionMultiplexer.GetDatabase();
        await database.StringSetAsync(key, JsonSerializer.Serialize(value), TimeSpan.FromMinutes(10));
    }

    public async Task<bool> DeleteKeyAsync(string key, CancellationToken token)
    {
        var database = connectionMultiplexer.GetDatabase();
        bool _isKeyExist = await database.KeyExistsAsync(key);
        if (_isKeyExist == true)
        {
            return await database.KeyDeleteAsync(key);
        }
        return false;
    }

    public async Task<string> TryAcquireIdempotencyAsync(
    string key,
    CancellationToken cancellationToken)
    {
        var database = connectionMultiplexer.GetDatabase();

        var script = """
        local value = redis.call('GET', KEYS[1])

        if not value then
            return 'NOT_FOUND'
        end

        local data = cjson.decode(value)

        if data.RequestStatus == 0 then
            data.RequestStatus = 1

            redis.call(
                'SET',
                KEYS[1],
                cjson.encode(data),
                'EX',
                600
            )

            return 'ACQUIRED'
        end

        if data.RequestStatus == 1 then
            return 'IN_PROGRESS'
        end

        if data.RequestStatus == 2 then
            return 'COMPLETED'
        end

        return 'UNKNOWN'
        """;

        var result = await database.ScriptEvaluateAsync(
            script,
            new RedisKey[] { key });

        return result.ToString();
    }
}