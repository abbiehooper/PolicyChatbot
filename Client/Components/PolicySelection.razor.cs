using Microsoft.AspNetCore.Components;
using PolicyChatbot.Shared.Models;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace PolicyChatbot.Client.Components;

public partial class PolicySelection : IDisposable
{
    [Inject]
    public required IAppStateManager AppStateManager { get; set; }

    [Inject]
    public required HttpClient Http { get; set; }

    private bool disposedValue;

    protected override async Task OnInitializedAsync()
    {
        AppStateManager.OnProductSelectedAsync += RefreshAsync;

        try
        {
            AppStateManager.InsuranceTypes = await Http.GetFromJsonAsync<List<string>>("api/policy/insurance-types") ?? [];
        }
        catch (Exception ex)
        {
            AppStateManager.ErrorMessage = $"Failed to load insurance types: {ex.Message}";
        }
    }

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

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                AppStateManager.OnProductSelectedAsync -= RefreshAsync;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void RefreshAsync(object? sender, EventArgs e) => StateHasChanged();
}
