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

    [Inject]
    public required IAppStateManager AppStateManager { get; set; }
    
    [Inject]
    public required HttpClient Http { get; set; }

    private string userInput = "";
    private bool isLoading = false;

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

    private void RefreshAsync(object? sender, EventArgs e) => StateHasChanged();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JS.InvokeVoidAsync("scrollToBottomById", "scrollContainer");
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(userInput) || isLoading)
            return;

        var question = userInput;
        userInput = "";
        AppStateManager.ErrorMessage = "";

        AddChatMessage(question, true);
        isLoading = true;

        try
        {
            var request = new ChatRequest
            {
                ProductId = AppStateManager.SelectedProductId,
                Question = question
            };

            var response = await Http.PostAsJsonAsync("api/chatbot/chat", request);

            if (response.IsSuccessStatusCode)
            {
                var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
                AddChatMessage(chatResponse?.Answer ?? "No response received", false);
            }
            else
            {
                AppStateManager.ErrorMessage = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            AppStateManager.ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
            await JS.InvokeVoidAsync("focusById", "userInputId");
        }
    }

    private void AddChatMessage(string content, bool isUser) => AppStateManager.ChatMessages.Add(new ChatMessage { Content = content, IsUser = isUser });

    private static string GetMessageClass(bool isUser) => isUser ? "user message" : "bot message";
}