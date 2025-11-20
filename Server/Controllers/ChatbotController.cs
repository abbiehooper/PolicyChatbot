using Microsoft.AspNetCore.Mvc;
using PolicyChatbot.Server.Services;
using PolicyChatbot.Shared.Models;
using System.Text.Json;

namespace PolicyChatbot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatbotController(IPolicyService policyService, IChatbotService chatbotService) : ControllerBase
{
    private readonly IPolicyService _policyService = policyService;
    private readonly IChatbotService _chatbotService = chatbotService;

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        var policy = _policyService.GetPolicyContent(request.ProductId);

        if (policy == null)
            return NotFound("Policy not found");

        var answer = await _chatbotService.GetClaudeResponseAsync(request.Question, policy);

        return Ok(new ChatResponse { Answer = answer });
    }
}