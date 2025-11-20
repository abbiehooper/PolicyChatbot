namespace PolicyChatbot.Server.Startup;
public class AnthropicOptions
{
    public const string SectionName = "Anthropic";
    public required string BaseUrl { get; set; }
    public required string ApiKey { get; set; }
}
