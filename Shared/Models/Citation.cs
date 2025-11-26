namespace PolicyChatbot.Shared.Models;

public class Citation
{
    public int PageNumber { get; set; }
    public string ExtractedText { get; set; } = "";
    public string HighlightText { get; set; } = "";
    public int CitationIndex { get; set; }
}
