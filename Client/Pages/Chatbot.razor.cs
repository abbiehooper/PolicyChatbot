using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PolicyChatbot.Shared.Models;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using static MudBlazor.CategoryTypes;

namespace PolicyChatbot.Client.Pages;

public partial class Chatbot
{

    [Inject]
    public required IJSRuntime JS { get; set; }

    private List<string> insuranceTypes = [];
    private List<string> availableInsurers = [];
    private List<ProductInfo> availableProducts = [];

    private string selectedInsuranceType = "";
    private string selectedInsurer = "";
    private string selectedProductId = "";

    private readonly List<ChatMessage> chatMessages = [];
    private string userInput = "";
    private bool isLoading = false;
    private string errorMessage = "";

    private string SelectedProductName =>
        availableProducts.Find(p => p.Id == selectedProductId)?.Name ?? "";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            insuranceTypes = await Http.GetFromJsonAsync<List<string>>("api/policy/insurance-types") ?? new();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load insurance types: {ex.Message}";
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JS.InvokeVoidAsync("scrollToBottomById", "scrollContainer");
    }

    private async Task OnInsuranceTypeChanged(string value)
    {
        selectedInsuranceType = value;
        selectedInsurer = "";
        selectedProductId = "";
        availableInsurers.Clear();
        availableProducts.Clear();
        chatMessages.Clear();
        errorMessage = "";

        if (!string.IsNullOrEmpty(selectedInsuranceType))
        {
            try
            {
                availableInsurers = await Http.GetFromJsonAsync<List<string>>($"api/policy/insurers?insuranceType={selectedInsuranceType}") ?? new();
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load insurers: {ex.Message}";
            }
        }
    }

    private async Task OnInsurerChanged(string value)
    {
        selectedInsurer = value;
        selectedProductId = "";
        availableProducts.Clear();
        chatMessages.Clear();
        errorMessage = "";

        if (!string.IsNullOrEmpty(selectedInsurer) && !string.IsNullOrEmpty(selectedInsuranceType))
        {
            try
            {
                availableProducts = await Http.GetFromJsonAsync<List<ProductInfo>>($"api/policy/products?insuranceType={selectedInsuranceType}&insurer={selectedInsurer}") ?? new();
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load products: {ex.Message}";
            }
        }
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(userInput) || isLoading)
            return;

        var question = userInput;
        userInput = "";
        errorMessage = "";

        chatMessages.Add(new ChatMessage { Content = question, IsUser = true });
        isLoading = true;

        try
        {
            var request = new ChatRequest
            {
                ProductId = selectedProductId,
                Question = question
            };

            var response = await Http.PostAsJsonAsync("api/chatbot/chat", request);

            if (response.IsSuccessStatusCode)
            {
                var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
                chatMessages.Add(new ChatMessage
                {
                    Content = chatResponse?.Answer ?? "No response received",
                    IsUser = false
                });
            }
            else
            {
                errorMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
            await JS.InvokeVoidAsync("focusById", "userInputId");
        }
    }

    private static 
        string GetMessageClass(bool isUser)
    {
        return isUser
            ? "user message"
            : "bot message";
    }
}