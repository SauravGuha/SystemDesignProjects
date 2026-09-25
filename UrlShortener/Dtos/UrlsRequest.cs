
using System.Text.Json;

namespace UrlShortener.Dtos;

public class UrlsRequest
{
    public required string Url { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}