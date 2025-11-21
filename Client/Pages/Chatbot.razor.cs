using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PolicyChatbot.Shared.Models;
using System.Net.Http.Json;

namespace PolicyChatbot.Client.Pages;

public partial class Chatbot
{
    private List<string> insuranceTypes = [];
    private List<string> availableInsurers = [];
    private List<ProductInfo> availableProducts = [];

    private string selectedInsuranceType = "";
    private string selectedInsurer = "";
    private string selectedProductId = "";

    private List<ChatMessage> chatMessages = [];
    private string userInput = "";
    private bool isLoading = false;
    private string errorMessage = "";

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
        }
    }

    private void HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !isLoading)
        {
            _ = SendMessage();
        }
    }

    private string GetMessageStyle(bool isUser)
    {
        return isUser
            ? "background-color: #e3f2fd; margin-left: 20%;"
            : "background-color: #ffffff; margin-right: 20%;";
    }

    private string GetLabel()
    {
        if(string.IsNullOrEmpty(selectedProductId))
            return "Select an insurance product to start chatting";

        return $"Ask a question about {selectedProductId}";
    }
}