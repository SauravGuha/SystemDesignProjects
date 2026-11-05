
namespace ChatApplication.Api.Models.DTOs;

public class MessageResponse
{
    public Guid Id { get; set; }

    public Guid SenderId { get; set; }

    public Guid ClientMessageId { get; set; }

    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; }
}