using System.Net.Http;
using System.Text.Json;

namespace PolicyChatbot.Server.Services;

public interface IChatbotService
{
    /// <summary>
    /// Asynchronously retrieves a response from the Claude AI model based on the provided question and policy content.
    /// </summary>
    /// <remarks>This method sends the specified question and policy content to the Claude AI model and
    /// returns the generated response. Ensure that both parameters are valid and provide sufficient context for
    /// meaningful results.</remarks>
    /// <param name="question">The question to be sent to the Claude AI model. This parameter cannot be null or empty.</param>
    /// <param name="policyContent">The policy content to provide additional context for the response. This parameter cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response from the Claude AI
    /// model as a string.</returns>
    Task<string> GetClaudeResponseAsync(string question, string policyContent);
}
public class ChatbotService(IHttpClientFactory httpClientFactory) : IChatbotService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    public async Task<string> GetClaudeResponseAsync(string question, string policyContent)
    {
        var httpClient = _httpClientFactory.CreateClient("Anthropic");

        var systemPrompt = BuildSystemPrompt(policyContent);
        var requestBody = BuildClaudeRequest(question, systemPrompt);

        var response = await httpClient.PostAsJsonAsync("messages", requestBody);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(jsonResponse);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();

        return content ?? "Sorry, I couldn't process that request.";
    }

    /// <summary>
    /// Constructs a system prompt based on the provided policy content.
    /// </summary>
    /// <param name="policyContent">The policy content to include in the system prompt. Cannot be null or empty.</param>
    /// <returns>A string representing the system prompt generated from the specified policy content.</returns>
    private static string BuildSystemPrompt(string policyContent) =>
        $@"You are a helpful insurance policy assistant. You ONLY answer questions about the specific insurance policy provided below.

            POLICY DOCUMENT:
            {policyContent}

            RULES:
            - Only answer questions based on the policy information provided above
            - If the policy doesn't mention something, clearly state 'This policy does not specify information about [topic]'
            - Be concise and specific
            - Quote relevant policy sections when appropriate
            - If asked about topics not in the policy, politely redirect to policy-related questions
            
            FORMATTING: 
            - Whenever you are quoting the policy wording the text should be in italics
        ";

    /// <summary>
    /// Constructs a request object for interacting with the Claude AI system.
    /// </summary>
    /// <param name="question">The user-provided question or input to be sent to the Claude AI system.</param>
    /// <param name="systemPrompt">The system-level prompt that defines the context or behavior for the Claude AI system. This parameter cannot be
    /// null or empty.</param>
    /// <returns>An object representing the request to be sent to the Claude AI system.</returns>
    private static object BuildClaudeRequest(string question, string systemPrompt) =>
        new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 1024,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = question }
            }
        };
}
