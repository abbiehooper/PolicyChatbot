namespace PolicyChatbot.Shared.Models;

public class ChatResponse
{
    public string Answer { get; set; } = "";
    public List<Citation> Citations { get; set; } = [];
}
