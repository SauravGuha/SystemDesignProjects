
namespace ChatApplication.Api.Models.Entities;

public class User
{
    public Guid Id { get; set; }

    public required string DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}