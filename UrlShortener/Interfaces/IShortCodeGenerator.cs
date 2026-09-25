

namespace UrlShortener.Interfaces;

public interface IShortCodeGenerator
{
    string GenerateShortCode(string url);
}