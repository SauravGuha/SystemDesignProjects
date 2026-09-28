
using UrlShortener.Models;

namespace UrlShortener.Interfaces;

public interface IApplicationCache
{
    Task<CacheModels<T>?> GetValueAsync<T>(string key, CancellationToken cancellationToken);

    Task SetValueAsync<T>(string key, CacheModels<T> value, CancellationToken cancellationToken);

    Task<bool> DeleteKeyAsync(string key, CancellationToken token);
}