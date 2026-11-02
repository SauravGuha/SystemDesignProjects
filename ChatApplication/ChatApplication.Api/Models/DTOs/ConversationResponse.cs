

namespace ChatApplication.Api.Models.DTOs;

public class ConversationResponse
{
    public Guid Id { get; set; }

    public required string ConversationType { get; set; }

    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public required string CreatedBy { get; set; }

    public Guid? DirectConversationUser1Id { get; set; }

    public Guid? DirectConversationUser2Id { get; set; }

    public IEnumerable<ConversationMemberResponse>? ConversationMemberResponses { get; set; }
}

public class ConversationMemberResponse
{
    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime LeftAt { get; set; }
}