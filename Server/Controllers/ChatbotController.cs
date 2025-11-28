using Microsoft.AspNetCore.Mvc;
using PolicyChatbot.Server.Services;
using PolicyChatbot.Shared.Models;

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
        var policyContent = _policyService.GetPolicyContentWithPages(request.ProductId);

        if (policyContent == null)
            return NotFound("Policy not found");

        // Use productId as conversationId - this keeps context per policy
        var conversationId = request.ProductId;

        var response = await _chatbotService.GetClaudeResponseWithCitationsAsync(
            request.Question,
            policyContent,
            conversationId);

        return Ok(response);
    }

    [HttpPost("clear-conversation")]
    public IActionResult ClearConversation([FromBody] ClearConversationRequest request)
    {
        _chatbotService.ClearConversation(request.ProductId);
        return Ok();
    }
}