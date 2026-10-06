
namespace UrlShortener.Interfaces;

public enum EventType
{
    DbUpdateConcurrencyException
}

public interface IConcurrencyPublisher
{
    Task PublishAsync(EventType eventType, string requestId, CancellationToken cancellationToken);
}