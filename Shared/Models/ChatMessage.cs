namespace PolicyChatbot.Shared.Models;

public class ChatMessage
{
    public string Content { get; set; } = "";
    public bool IsUser { get; set; }
    public List<Citation> Citations { get; set; } = [];
}
