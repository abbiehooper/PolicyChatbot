namespace PolicyChatbot.Shared.Models;

public class PolicyContent
{
    public string FullText { get; set; } = "";
    public List<PolicyPage> Pages { get; set; } = [];
}
