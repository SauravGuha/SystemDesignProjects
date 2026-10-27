
namespace ChatApplication.Api.Models.Entities;

public class IdempotencyRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public required string Operation { get; set; }

    public required string Key { get; set; }

    public required string RequestHash { get; set; }

    public int ResponseStatusCode { get; set; }

    public required string ResponseBody { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TimeSpan ExpiresAt { get; set; } = new TimeSpan(0, 5, 0);
}