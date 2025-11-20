using Microsoft.AspNetCore.Mvc;
using PolicyChatbot.Server.Services;
using PolicyChatbot.Shared;
using PolicyChatbot.Shared.Models;
using System.Text.Json;

namespace PolicyChatbot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PolicyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly PolicyRepository _policyRepo;

    public PolicyController(IHttpClientFactory httpClientFactory, IConfiguration configuration, PolicyRepository policyRepo)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _policyRepo = policyRepo;
    }

    [HttpGet("insurance-types")]
    public ActionResult<List<string>> GetInsuranceTypes()
    {
        return Ok(_policyRepo.GetInsuranceTypes());
    }

    [HttpGet("insurers")]
    public ActionResult<List<string>> GetInsurers([FromQuery] string insuranceType)
    {
        return Ok(_policyRepo.GetInsurers(insuranceType));
    }

    [HttpGet("products")]
    public ActionResult<List<ProductInfo>> GetProducts([FromQuery] string insuranceType, [FromQuery] string insurer)
    {
        return Ok(_policyRepo.GetProducts(insuranceType, insurer));
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        var policy = _policyRepo.GetPolicyContent(request.ProductId);

        if (policy == null)
            return NotFound("Policy not found");

        var answer = await GetClaudeResponse(request.Question, policy);

        return Ok(new ChatResponse { Answer = answer });
    }

    private async Task<string> GetClaudeResponse(string question, string policyContent)
    {
        var apiKey = _configuration["Anthropic:ApiKey"];

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

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(requestBody)
        };

        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await client.SendAsync(request);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(jsonResponse);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();

        return content ?? "Sorry, I couldn't process that request.";
    }
}