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
/// 
/// This implementation uses Claude's Prompt Caching feature which:
/// 1. Caches the policy document in Claude's context
/// 2. Only charges for new tokens (the question) on subsequent requests
/// 3. Reduces costs by up to 90% for repeated queries on the same policy
/// 4. Maintains conversation history for contextual responses
/// </summary>
public class ChatbotService(IHttpClientFactory httpClientFactory, ILogger<ChatbotService> logger) : IChatbotService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ChatbotService> _logger = logger;

    // Store conversation histories by conversationId
    private static readonly ConcurrentDictionary<string, ConversationContext> _conversations = new();

    // Cleanup old conversations periodically
    private static readonly Timer _cleanupTimer = new(CleanupOldConversations, null,
        TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

    public async Task<ChatResponse> GetClaudeResponseWithCitationsAsync(
        string question,
        PolicyContent policyContent,
        string conversationId)
    {
        var httpClient = _httpClientFactory.CreateClient("Anthropic");

        // Get or create conversation context
        var context = _conversations.GetOrAdd(conversationId, _ => new ConversationContext
        {
            PolicyContent = policyContent,
            Messages = new List<ConversationMessage>(),
            IsFirstMessage = true
        });

        // Update last accessed time
        context.LastAccessed = DateTime.UtcNow;

        // Build request with prompt caching
        var requestBody = context.IsFirstMessage
            ? BuildInitialRequestWithCache(question, policyContent, context.Messages)
            : BuildFollowUpRequest(question, policyContent, context.Messages);

        var response = await httpClient.PostAsJsonAsync("messages", requestBody);
        var jsonResponse = await response.Content.ReadAsStringAsync();

        // Log cache usage for monitoring
        LogCacheUsage(jsonResponse);

        var doc = JsonDocument.Parse(jsonResponse);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString()
            ?? "Sorry, I couldn't process that request.";

        var chatResponse = ParseResponseWithCitations(content);

        // Store the conversation history (only user question and assistant response, not the policy)
        context.Messages.Add(new ConversationMessage { Role = "user", Content = question });
        context.Messages.Add(new ConversationMessage { Role = "assistant", Content = chatResponse.Answer });
        context.IsFirstMessage = false;

        // Limit history to last 20 messages (10 exchanges) to prevent context getting too large
        if (context.Messages.Count > 20)
        {
            context.Messages.RemoveRange(0, context.Messages.Count - 20);
        }

        return chatResponse;
    }

    public void ClearConversation(string conversationId)
    {
        _conversations.TryRemove(conversationId, out _);
        _logger.LogInformation("Cleared conversation: {ConversationId}", conversationId);
    }

    /// <summary>
    /// Build the initial request with prompt caching.
    /// The policy document is marked for caching, so subsequent requests won't re-send it.
    /// </summary>
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
                cache_control = new { type = "ephemeral" } // Mark for caching
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

    /// <summary>
    /// Build follow-up requests. The cached policy content is automatically reused.
    /// </summary>
    private static object BuildFollowUpRequest(
        string question,
        PolicyContent policyContent,
        List<ConversationMessage> history)
    {
        // Same structure as initial request - caching happens automatically
        return BuildInitialRequestWithCache(question, policyContent, history);
    }

    private static List<object> BuildMessagesWithHistory(List<ConversationMessage> history, string newQuestion)
    {
        var messages = new List<object>();

        // Add conversation history (without the policy - that's in system prompt)
        foreach (var msg in history)
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        // Add new question
        messages.Add(new { role = "user", content = newQuestion });

        return messages;
    }

    private void LogCacheUsage(string jsonResponse)
    {
        try
        {
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

    private static void CleanupOldConversations(object? state)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-2); // Remove conversations older than 2 hours
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
