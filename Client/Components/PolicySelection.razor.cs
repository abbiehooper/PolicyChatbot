using Microsoft.AspNetCore.Components;
using PolicyChatbot.Shared.Models;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace PolicyChatbot.Client.Components;

public partial class PolicySelection
{
    [Inject]
    public required IAppStateManager AppStateManager { get; set; }

    [Inject]
    public required HttpClient Http { get; set; }

    private async Task OnInsuranceTypeChanged(string value)
    {
        AppStateManager.OnInsuranceTypeChanged(value);

        if (!string.IsNullOrEmpty(AppStateManager.SelectedInsuranceType))
        {
            try
            {
                AppStateManager.AvailableInsurers = await Http.GetFromJsonAsync<List<string>>($"api/policy/insurers?insuranceType={AppStateManager.SelectedInsuranceType}") ?? new();
            }
            catch (Exception ex)
            {
                AppStateManager.ErrorMessage = $"Failed to load insurers: {ex.Message}";
            }
        }
    }

    private async Task OnInsurerChanged(string value)
    {
        AppStateManager.OnInsurerChanged(value);

        if (!string.IsNullOrEmpty(AppStateManager.SelectedInsurer) && !string.IsNullOrEmpty(AppStateManager.SelectedInsuranceType))
        {
            try
            {
                AppStateManager.AvailableProducts = await Http.GetFromJsonAsync<List<ProductInfo>>($"api/policy/products?insuranceType={AppStateManager.SelectedInsuranceType}&insurer={AppStateManager.SelectedInsurer}") ?? [];
            }
            catch (Exception ex)
            {
                AppStateManager.ErrorMessage = $"Failed to load products: {ex.Message}";
            }
        }
    }

    private void OnProductChanged(string value)
    {
        AppStateManager.SelectedProductId = value;
        AppStateManager.InvokeOnProductSelected();
    }
}
