using System.Text.Json;
using System.Text.RegularExpressions;
using PolicyChatbot.Shared.Models;

namespace PolicyChatbot.Server.Services;

public interface IChatbotService
{
    Task<ChatResponse> GetClaudeResponseWithCitationsAsync(string question, PolicyContent policyContent);
}

public class ChatbotService(IHttpClientFactory httpClientFactory) : IChatbotService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<ChatResponse> GetClaudeResponseWithCitationsAsync(string question, PolicyContent policyContent)
    {
        var httpClient = _httpClientFactory.CreateClient("Anthropic");

        var systemPrompt = BuildSystemPromptWithCitations(policyContent);
        var requestBody = BuildClaudeRequest(question, systemPrompt);

        var response = await httpClient.PostAsJsonAsync("messages", requestBody);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(jsonResponse);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString()
            ?? "Sorry, I couldn't process that request.";

        return ParseResponseWithCitations(content, policyContent);
    }

    private static string BuildSystemPromptWithCitations(PolicyContent policyContent)
    {
        var pagesContext = string.Join("\n\n", policyContent.Pages.Select(p =>
            $"=== PAGE {p.PageNumber} ===\n{p.Content}"));

        return $@"You are a helpful insurance policy assistant. You ONLY answer questions about the specific insurance policy provided below.

POLICY DOCUMENT (with page numbers):
{pagesContext}

RULES:
- Only answer questions based on the policy information provided above
- If the policy doesn't mention something, clearly state 'This policy does not specify information about [topic]'
- Be concise and specific
- If asked about topics not in the policy, politely redirect to policy-related questions

CRITICAL CITATION FORMAT:
When you quote or reference specific text from the policy, you MUST use this exact format:
[CITE:page_number:""exact quoted text""]

For example:
- The policy states [CITE:5:""the excess is £250 for all claims""] which means you would pay this amount first.
- According to [CITE:12:""fire damage is covered under Section 3""], your property is protected.

IMPORTANT:
- Always include the page number where you found the information
- Quote the exact text from that page (or a relevant portion)
- You can have multiple citations in one response
- Every factual claim about the policy should have a citation
- The quoted text should be word-for-word from the policy

FORMATTING: 
- Keep your explanations clear and helpful
- Use the citation format for all policy references";
    }

    private static object BuildClaudeRequest(string question, string systemPrompt) =>
        new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 2048,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = question }
            }
        };

    private static ChatResponse ParseResponseWithCitations(string rawResponse, PolicyContent policyContent)
    {
        var citations = new List<Citation>();
        var citationIndex = 0;

        // Pattern to match [CITE:page_number:"quoted text"]
        var citationPattern = new Regex(@"\[CITE:(\d+):""([^""]+)""\]", RegexOptions.Compiled);

        var processedAnswer = citationPattern.Replace(rawResponse, match =>
        {
            citationIndex++;
            var pageNumber = int.Parse(match.Groups[1].Value);
            var quotedText = match.Groups[2].Value;

            citations.Add(new Citation
            {
                CitationIndex = citationIndex,
                PageNumber = pageNumber,
                ExtractedText = quotedText,
                HighlightText = quotedText
            });

            // Replace with a superscript-style citation marker
            return $"<cite data-citation=\"{citationIndex}\">[{citationIndex}]</cite>";
        });

        return new ChatResponse
        {
            Answer = processedAnswer,
            Citations = citations
        };
    }
}
