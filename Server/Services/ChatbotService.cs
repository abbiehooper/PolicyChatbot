using PolicyChatbot.Shared.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PolicyChatbot.Server.Services;

public interface IChatbotService
{
    Task<ChatResponse> GetClaudeResponseWithCitationsAsync(string question, PolicyContent policyContent, string conversationId);
    void ClearConversation(string conversationId);
}

/// <summary>
/// ChatbotService with Prompt Caching support for cost optimization.
/// </summary>
public class ChatbotService(IHttpClientFactory httpClientFactory, ILogger<ChatbotService> logger) : IChatbotService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ChatbotService> _logger = logger;

    private static readonly ConcurrentDictionary<string, ConversationContext> _conversations = new();

    private static readonly Timer _cleanupTimer = new(CleanupOldConversations, null,
        TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

    public async Task<ChatResponse> GetClaudeResponseWithCitationsAsync(
        string question,
        PolicyContent policyContent,
        string conversationId)
    {
        var httpClient = _httpClientFactory.CreateClient("Anthropic");

        var context = _conversations.GetOrAdd(conversationId, _ => new ConversationContext
        {
            PolicyContent = policyContent,
            Messages = new List<ConversationMessage>(),
            IsFirstMessage = true
        });

        context.LastAccessed = DateTime.UtcNow;

        var requestBody = context.IsFirstMessage
            ? BuildInitialRequestWithCache(question, policyContent, context.Messages)
            : BuildFollowUpRequest(question, policyContent, context.Messages);

        try
        {
            var response = await httpClient.PostAsJsonAsync("messages", requestBody);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            // Log the response for debugging
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Anthropic API error. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, jsonResponse);
                throw new HttpRequestException($"Anthropic API returned {response.StatusCode}: {jsonResponse}");
            }

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                _logger.LogError("Anthropic API returned empty response");
                throw new HttpRequestException("Anthropic API returned empty response");
            }

            LogCacheUsage(jsonResponse);

            var doc = JsonDocument.Parse(jsonResponse);

            if (!doc.RootElement.TryGetProperty("content", out var contentArray) ||
                contentArray.GetArrayLength() == 0)
            {
                _logger.LogError("Unexpected API response structure: {Response}", jsonResponse);
                throw new HttpRequestException("Unexpected API response structure");
            }

            var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString()
                ?? "Sorry, I couldn't process that request.";

            var chatResponse = ParseResponseWithCitations(content);

            context.Messages.Add(new ConversationMessage { Role = "user", Content = question });
            context.Messages.Add(new ConversationMessage { Role = "assistant", Content = chatResponse.Answer });
            context.IsFirstMessage = false;

            if (context.Messages.Count > 20)
            {
                context.Messages.RemoveRange(0, context.Messages.Count - 20);
            }

            return chatResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Anthropic API");
            throw new HttpRequestException($"Failed to connect to Anthropic API: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Anthropic API response");
            throw new HttpRequestException($"Invalid response from Anthropic API: {ex.Message}");
        }
    }

    public void ClearConversation(string conversationId)
    {
        _conversations.TryRemove(conversationId, out _);
        _logger.LogInformation("Cleared conversation: {ConversationId}", conversationId);
    }

    private static object BuildInitialRequestWithCache(
        string question,
        PolicyContent policyContent,
        List<ConversationMessage> history)
    {
        var pagesContext = string.Join("\n\n", policyContent.Pages.Select(p =>
            $"=== PAGE {p.PageNumber} ===\n{p.Content}"));

        var systemBlocks = new List<object>
        {
            new
            {
                type = "text",
                text = @"You are a helpful insurance policy assistant. You ONLY answer questions about the specific insurance policy provided in the next message.

                    RULES:
                    - Only answer questions based on the policy information provided
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
                    - Use the citation format for all policy references"
            },
            new
            {
                type = "text",
                text = $"POLICY DOCUMENT (with page numbers):\n\n{pagesContext}",
                cache_control = new { type = "ephemeral" }
            }
        };

        var messages = BuildMessagesWithHistory(history, question);

        return new
        {
            model = "claude-haiku-4-5-20251001",
            max_tokens = 1500,
            system = systemBlocks,
            messages = messages
        };
    }

    private static object BuildFollowUpRequest(
        string question,
        PolicyContent policyContent,
        List<ConversationMessage> history)
    {
        return BuildInitialRequestWithCache(question, policyContent, history);
    }

    private static List<object> BuildMessagesWithHistory(List<ConversationMessage> history, string newQuestion)
    {
        var messages = new List<object>();

        foreach (var msg in history)
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        messages.Add(new { role = "user", content = newQuestion });

        return messages;
    }

    private void LogCacheUsage(string jsonResponse)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                _logger.LogWarning("Empty response, cannot parse cache usage");
                return;
            }

            var doc = JsonDocument.Parse(jsonResponse);
            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                var inputTokens = usage.TryGetProperty("input_tokens", out var inTokens) ? inTokens.GetInt32() : 0;
                var cacheCreationTokens = usage.TryGetProperty("cache_creation_input_tokens", out var createTokens)
                    ? createTokens.GetInt32() : 0;
                var cacheReadTokens = usage.TryGetProperty("cache_read_input_tokens", out var readTokens)
                    ? readTokens.GetInt32() : 0;

                if (cacheCreationTokens > 0)
                {
                    _logger.LogInformation("Cache created: {Tokens} tokens", cacheCreationTokens);
                }
                if (cacheReadTokens > 0)
                {
                    _logger.LogInformation("Cache hit: {Tokens} tokens read from cache", cacheReadTokens);
                }

                _logger.LogDebug("Token usage - Input: {Input}, Cache Creation: {Create}, Cache Read: {Read}",
                    inputTokens, cacheCreationTokens, cacheReadTokens);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse cache usage");
        }
    }

    private static ChatResponse ParseResponseWithCitations(string rawResponse)
    {
        var citations = new List<Citation>();
        var citationIndex = 0;

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

            return $"<cite data-citation=\"{citationIndex}\">[{citationIndex}]</cite>";
        });

        return new ChatResponse
        {
            Answer = processedAnswer,
            Citations = citations
        };
    }

    private static void CleanupOldConversations(object? state)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-2);
        var toRemove = _conversations
            .Where(kvp => kvp.Value.LastAccessed < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _conversations.TryRemove(key, out _);
        }
    }

    private class ConversationContext
    {
        public PolicyContent PolicyContent { get; set; } = new();
        public List<ConversationMessage> Messages { get; set; } = new();
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
        public bool IsFirstMessage { get; set; } = true;
    }

    private class ConversationMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }
}