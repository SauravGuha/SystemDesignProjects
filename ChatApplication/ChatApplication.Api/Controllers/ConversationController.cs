
using ChatApplication.Api.Models.DTOs;
using ChatApplication.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

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

    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupConversationRequest request, CancellationToken token)
    {
        //obtain the request id from header
        var requestid = this.HttpContext.Request.Headers["Request-Id"];
        if (StringValues.IsNullOrEmpty(requestid))
        {
            return BadRequest("Request-Id is absent");
        }
        var conversation = await conversationService.CreateGroupConversation(request, requestid[0]!, nameof(CreateGroup), token);
        return Ok(conversation);
    }


    [HttpGet("{userid}")]
    public async Task<IActionResult> GetConversations(string userid, CancellationToken token)
    {
        var result = await conversationService.UserConversations(userid, token);
        return Ok(result);
    }


    [HttpGet("message/{userId}/{userId2}")]
    public async Task<IActionResult> GetMessages(Guid userId, Guid userId2, CancellationToken token,
    [FromQuery] string next = "")
    {
        var result = await conversationService.GetDirectMessages(userId, userId2, token, next);
        return Ok(result);
    }
}