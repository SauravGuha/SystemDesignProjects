
namespace UrlShortener.Interfaces;

public interface IApplicationCache
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken);

    Task SetValueAsync(string key, string value, CancellationToken cancellationToken);

    Task<Boolean> DeleteKeyAsync(string key, CancellationToken token);
}