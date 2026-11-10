
namespace ChatApplication.Api.Models.DTOs;

public class SendMessageRequest
{
    public Guid SenderId { get; set; }

    public Guid ClientMessageId { get; set; }

    public string Content { get; set; } = null!;
}