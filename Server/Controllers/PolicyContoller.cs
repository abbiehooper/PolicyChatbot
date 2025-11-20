using Microsoft.AspNetCore.Mvc;
using PolicyChatbot.Server.Services;
using PolicyChatbot.Shared.Models;
using System.Text.Json;

namespace PolicyChatbot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PolicyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IPolicyRepository _policyRepository;

    public PolicyController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IPolicyRepository policyRepository)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _policyRepository = policyRepository;
    }

    [HttpGet("insurance-types")]
    public ActionResult<List<string>> GetInsuranceTypes()
    {
        return Ok(_policyRepository.GetInsuranceTypes());
    }

    [HttpGet("insurers")]
    public ActionResult<List<string>> GetInsurers([FromQuery] string insuranceType)
    {
        return Ok(_policyRepository.GetInsurers(insuranceType));
    }

    [HttpGet("products")]
    public ActionResult<List<ProductInfo>> GetProducts([FromQuery] string insuranceType, [FromQuery] string insurer)
    {
        return Ok(_policyRepository.GetProducts(insuranceType, insurer));
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        var policy = _policyRepository.GetPolicyContent(request.ProductId);

        if (policy == null)
            return NotFound("Policy not found");

        var answer = await GetClaudeResponseAsync(request.Question, policy);

        return Ok(new ChatResponse { Answer = answer });
    }

    private async Task<string> GetClaudeResponseAsync(string question, string policyContent)
    {
        var httpClient = _httpClientFactory.CreateClient("Anthropic");

        var systemPrompt = $@"You are a helpful insurance policy assistant. You ONLY answer questions about the specific insurance policy provided below.

            POLICY DOCUMENT:
            {policyContent}

            RULES:
            - Only answer questions based on the policy information provided above
            - If the policy doesn't mention something, clearly state 'This policy does not specify information about [topic]'
            - Be concise and specific
            - Quote relevant policy sections when appropriate
            - If asked about topics not in the policy, politely redirect to policy-related questions";

        var requestBody = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 1024,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = question }
            }
        };

        var response = await httpClient.PostAsJsonAsync("messages", requestBody);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(jsonResponse);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();

        return content ?? "Sorry, I couldn't process that request.";
    }
}