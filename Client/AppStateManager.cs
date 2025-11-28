using PolicyChatbot.Shared.Models;
using System.Net.Http.Json;

namespace PolicyChatbot.Client;

public interface IAppStateManager
{
    List<string> InsuranceTypes { get; set; }
    List<string> AvailableInsurers { get; set; }
    List<ProductInfo> AvailableProducts { get; set; }
    string SelectedInsuranceType { get; set; }
    string SelectedInsurer { get; set; }
    string SelectedProductId { get; set; }
    string SelectedProductName { get; }
    Task OnInsuranceTypeChanged(string value);
    Task OnInsurerChanged(string value);
    List<ChatMessage> ChatMessages { get; }
    string ErrorMessage { get; set; }
    EventHandler? OnProductSelectedAsync { get; set; }
    void InvokeOnProductSelected();
    void StartNewChat();

    // Citation state
    bool IsPdfViewerOpen { get; set; }
    Citation? SelectedCitation { get; set; }
    void ShowCitation(Citation citation);
    void ClosePdfViewer();
    EventHandler? OnCitationSelected { get; set; }
}

public class AppStateManager(HttpClient http, ILogger<AppStateManager> logger) : IAppStateManager
{
    public List<string> InsuranceTypes { get; set; } = [];
    public List<string> AvailableInsurers { get; set; } = [];
    public List<ProductInfo> AvailableProducts { get; set; } = [];
    public string SelectedInsuranceType { get; set; } = "";
    public string SelectedInsurer { get; set; } = "";
    public string SelectedProductId { get; set; } = "";
    public string SelectedProductName => AvailableProducts.Find(p => p.Id == SelectedProductId)?.Name ?? "";
    public List<ChatMessage> ChatMessages { get; } = [];
    public string ErrorMessage { get; set; } = "";

    // Citation state
    public bool IsPdfViewerOpen { get; set; } = false;
    public Citation? SelectedCitation { get; set; }
    public EventHandler? OnCitationSelected { get; set; }

    public async Task OnInsuranceTypeChanged(string value)
    {
        SelectedInsuranceType = value;
        AvailableInsurers.Clear();
        await OnInsurerChanged("");
    }

    public async Task OnInsurerChanged(string value)
    {
        SelectedInsurer = value;

        if (!string.IsNullOrEmpty(SelectedProductId))
        {
            try
            {
                await http.PostAsJsonAsync("api/chatbot/clear-conversation",
                    new ClearConversationRequest { ProductId = SelectedProductId });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to clear conversation for ProductId: {ProductId}", SelectedProductId);
            }
        }

        SelectedProductId = "";
        AvailableProducts.Clear();
        ChatMessages.Clear();
        ErrorMessage = "";
        ClosePdfViewer();
    }

    public EventHandler? OnProductSelectedAsync { get; set; }

    public void InvokeOnProductSelected() => OnProductSelectedAsync?.Invoke(this, EventArgs.Empty);

    public void ShowCitation(Citation citation)
    {
        SelectedCitation = citation;
        IsPdfViewerOpen = true;
        OnCitationSelected?.Invoke(this, EventArgs.Empty);
    }

    public void ClosePdfViewer()
    {
        IsPdfViewerOpen = false;
        SelectedCitation = null;
        OnCitationSelected?.Invoke(this, EventArgs.Empty);
    }

    public void StartNewChat()
    {
        SelectedInsuranceType = "";
        SelectedInsurer = "";
        SelectedProductId = "";
        AvailableInsurers.Clear();
        AvailableProducts.Clear();
        ChatMessages.Clear();
        ErrorMessage = "";
        ClosePdfViewer();

        // Trigger UI refresh
        InvokeOnProductSelected();
    }
}
