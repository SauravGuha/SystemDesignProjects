
using UrlShortener.Models;

namespace UrlShortener.Interfaces;

public interface IApplicationCache
{
    Task<CacheModels<T>?> GetValueAsync<T>(string key, CancellationToken cancellationToken);

    Task SetValueAsync<T>(string key, CacheModels<T> value, CancellationToken cancellationToken);

    Task<bool> DeleteKeyAsync(string key, CancellationToken token);

    Task<string> TryAcquireIdempotencyAsync(
    string key,
    CancellationToken cancellationToken);

    Task<string?> AcquireLockAsync(
    string key,
    TimeSpan expiry,
    CancellationToken cancellationToken);

    Task ReleaseLockAsync(
        string key,
        string token,
        CancellationToken cancellationToken);
}