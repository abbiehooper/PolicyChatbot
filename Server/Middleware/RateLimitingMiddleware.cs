using System.Collections.Concurrent;

namespace PolicyChatbot.Server.Middleware;

/// <summary>
/// Quick Win #7: Simple rate limiter to prevent excessive API usage
/// Limits requests per IP address to control costs
/// </summary>
public class RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RateLimitingMiddleware> _logger = logger;

    // Configuration
    private const int MaxRequestsPerMinute = 10;
    private const int MaxRequestsPerHour = 50;

    // Track requests by IP
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();

    public async Task InvokeAsync(HttpContext context)
    {
        // Only rate limit the chat endpoint
        if (!context.Request.Path.StartsWithSegments("/api/chatbot/chat"))
        {
            await _next(context);
            return;
        }

        var clientIp = GetClientIp(context);
        var rateLimitInfo = _rateLimits.GetOrAdd(clientIp, _ => new RateLimitInfo());

        // Clean up old entries periodically
        CleanupOldEntries();

        // Check rate limits
        var now = DateTime.UtcNow;

        lock (rateLimitInfo)
        {
            // Remove requests older than 1 hour
            rateLimitInfo.RequestTimes.RemoveAll(t => (now - t).TotalHours > 1);

            var requestsLastMinute = rateLimitInfo.RequestTimes.Count(t => (now - t).TotalMinutes <= 1);
            var requestsLastHour = rateLimitInfo.RequestTimes.Count;

            if (requestsLastMinute >= MaxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded (per minute) for IP: {ClientIp}", clientIp);
                context.Response.StatusCode = 429;
                context.Response.Headers.RetryAfter = "60";
                context.Response.WriteAsJsonAsync(new { error = "Too many requests. Please wait a minute." });
                return;
            }

            if (requestsLastHour >= MaxRequestsPerHour)
            {
                _logger.LogWarning("Rate limit exceeded (per hour) for IP: {ClientIp}", clientIp);
                context.Response.StatusCode = 429;
                context.Response.Headers.RetryAfter = "3600";
                context.Response.WriteAsJsonAsync(new { error = "Hourly limit reached. Please try again later." });
                return;
            }

            // Record this request
            rateLimitInfo.RequestTimes.Add(now);
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        // Check for forwarded IP (if behind a proxy)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static void CleanupOldEntries()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = _rateLimits
            .Where(kvp => !kvp.Value.RequestTimes.Any() ||
                         (now - kvp.Value.RequestTimes.Max()).TotalHours > 2)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _rateLimits.TryRemove(key, out _);
        }
    }

    private class RateLimitInfo
    {
        public List<DateTime> RequestTimes { get; } = [];
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
