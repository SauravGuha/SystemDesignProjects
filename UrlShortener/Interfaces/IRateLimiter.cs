
namespace UrlShortener.Interfaces;

public interface IRateLimiter
{
    Task<bool> IsAllowed(string key);
}