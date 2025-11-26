using PolicyChatbot.Shared.Models;

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
    void OnInsuranceTypeChanged(string value);
    void OnInsurerChanged(string value);
    List<ChatMessage> ChatMessages { get; } 
    string ErrorMessage { get; set; }
    EventHandler? OnProductSelectedAsync { get; set; }
    void InvokeOnProductSelected();
}

public class AppStateManager : IAppStateManager
{
    public List<string> InsuranceTypes { get ; set; } = [];
    public List<string> AvailableInsurers { get; set; } = [];
    public List<ProductInfo> AvailableProducts { get; set; } = [];
    public string SelectedInsuranceType { get; set; } = "";
    public string SelectedInsurer { get; set; } = "";
    public string SelectedProductId { get; set; } = "";
    public string SelectedProductName => AvailableProducts.Find(p => p.Id == SelectedProductId)?.Name ?? "";
    public List<ChatMessage> ChatMessages { get; } = [];
    public string ErrorMessage { get; set; } = "";

    public void OnInsuranceTypeChanged(string value)
    {
        SelectedInsuranceType = value;
        AvailableInsurers.Clear();
        OnInsurerChanged("");
    }

    public void OnInsurerChanged(string value)
    {
        SelectedInsurer = value;
        SelectedProductId = "";
        AvailableProducts.Clear();
        ChatMessages.Clear();
        ErrorMessage = "";
    }

    public EventHandler? OnProductSelectedAsync { get; set; }

    public void InvokeOnProductSelected() => OnProductSelectedAsync?.Invoke(this, EventArgs.Empty);
}
