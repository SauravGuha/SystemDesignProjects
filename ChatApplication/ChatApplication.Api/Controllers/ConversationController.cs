
using ChatApplication.Api.Models.DTOs;
using ChatApplication.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

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
        if (requestid.IsNullOrEmpty())
        {
            return BadRequest("Request-Id is absent");
        }
        var conversation = await conversationService.CreateGroupConversation(request, requestid[0]!, token);
        return Ok(conversation);
    }


    [HttpGet("conversations/{userid}")]
    public async Task<IActionResult> GetConversations(string userid, CancellationToken token)
    {
        var result = await conversationService.UserConversations(userid, token);
        return Ok(result);
    }
}