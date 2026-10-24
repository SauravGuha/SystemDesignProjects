
using ChatApplication.Api.Models.DTOs;
using ChatApplication.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.Api.Controllers;

[ApiController]
[Route("api/{controller}")]
public class ConversationController : ControllerBase
{
    private readonly ConversationService conversationService;

    public ConversationController(ConversationService conversationService)
    {
        this.conversationService = conversationService;
    }

    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirect([FromBody] CreateDirectConversationRequest request, CancellationToken token)
    {
        var conversation = await conversationService.CreateDirectConversation(request.UserId, request.OtherUserId, token);
        return Ok(conversation);
    }
}