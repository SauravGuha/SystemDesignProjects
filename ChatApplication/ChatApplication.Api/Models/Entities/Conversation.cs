
namespace ChatApplication.Api.Models.Entities;

public enum ConversationType
{
    Direct,
    Group
}

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ConversationType ConversationType { get; set; }

    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public required string CreatedBy { get; set; }

    public Guid? DirectConversationUser1Id { get; set; }

    public Guid? DirectConversationUser2Id { get; set; }
}