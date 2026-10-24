
namespace ChatApplication.Api.Models.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ConversationId { get; set; }

    /// <summary>
    /// User id who sent the message
    /// </summary>
    public Guid SenderId { get; set; }

    public Guid ClientMessageId { get; set; }

    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Sender { get; set; } = null!;

}