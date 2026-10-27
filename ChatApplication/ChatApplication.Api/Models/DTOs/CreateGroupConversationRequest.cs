
namespace ChatApplication.Api.Models.DTOs;

public class CreateGroupConversationRequest
{
    public Guid UserId { get; set; }

    public required string GroupName { get; set; }

    public required IEnumerable<Guid> Members { get; set; }
}