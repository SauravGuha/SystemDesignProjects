
namespace ChatApplication.Api.Models.DTOs;

public class CreateDirectConversationRequest
{
    public Guid UserId { get; set; }

    public Guid OtherUserId { get; set; }
}