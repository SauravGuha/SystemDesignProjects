
namespace ChatApplication.Api.Models.Entities;

public class ConversationMember
{
    public Guid ConversationId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public DateTime LeftAt { get; set; } = DateTime.MinValue;

}