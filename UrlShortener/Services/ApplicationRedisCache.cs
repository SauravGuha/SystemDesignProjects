
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
    private IDatabase _database;

    public ApplicationRedisCache(IConfiguration configuration)
    {
        this.connectionString = configuration.GetConnectionString("redissocket");
        if (string.IsNullOrWhiteSpace(this.connectionString))
            throw new ArgumentNullException("redissocket connection not found");
        this.connectionMultiplexer = ConnectionMultiplexer.Connect(this.connectionString);
        this._database = connectionMultiplexer.GetDatabase();
    }
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        var data = await this._database.StringGetAsync(key);
        return data;
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        await this._database.StringSetAsync(key, value);
    }

    public async Task<Boolean> DeleteKeyAsync(string key, CancellationToken token)
    {
        bool _isKeyExist = await _database.KeyExistsAsync(key);
        if (_isKeyExist == true)
        {
            return await _database.KeyDeleteAsync(key);
        }
        return false;
    }
}