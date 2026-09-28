
namespace UrlShortener.Models;

public enum RequestStatus
{
    Pending,
    Processing,
    Completed
}

public class CacheModels<T>
{
    public RequestStatus RequestStatus { get; set; } = RequestStatus.Processing;

    public required T Value { get; set; }
}