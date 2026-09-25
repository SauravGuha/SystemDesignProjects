
using System.Security.Cryptography;
using System.Text;
using UrlShortener.Interfaces;

namespace UrlShortener.Services;

public class HashShortCodeGenerator : IShortCodeGenerator
{
    public string GenerateShortCode(string url)
    {
        var urlBytes = Encoding.UTF8.GetBytes(url);
        var hashBytes = SHA256.HashData(urlBytes);
        return Convert.ToHexString(hashBytes);
    }
}