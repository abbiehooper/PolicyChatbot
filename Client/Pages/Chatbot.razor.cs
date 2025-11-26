using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PolicyChatbot.Shared.Models;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

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
        AppStateManager.OnCitationSelected += RefreshAsync;

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

        AddChatMessage(question, true, []);
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
                if (chatResponse != null)
                {
                    AddChatMessage(chatResponse.Answer, false, chatResponse.Citations);
                    AppStateManager.CurrentCitations = chatResponse.Citations;
                }
                else
                {
                    AddChatMessage("No response received", false, []);
                }
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

    private void AddChatMessage(string content, bool isUser, List<Citation> citations) => 
        AppStateManager.ChatMessages.Add(new ChatMessage 
        { 
            Content = content, 
            IsUser = isUser,
            Citations = citations
        });

    private static string GetMessageClass(bool isUser) => isUser ? "user message" : "bot message";

    private static string RenderMessageWithCitations(ChatMessage message)
    {
        var html = Markdown.ToHtml(message.Content);

        // Replace citation markers with styled spans
        var citationPattern = new Regex(@"<cite data-citation=""(\d+)"">\[(\d+)\]</cite>");
        html = citationPattern.Replace(html, match =>
        {
            var index = match.Groups[1].Value;
            return $"<span class=\"citation-marker\" data-citation=\"{index}\">[{index}]</span>";
        });

        return html;
    }

    private void HandleCitationClick(Citation citation)
    {
        AppStateManager.ShowCitation(citation);
        StateHasChanged();
    }

    private void ClosePdfViewer()
    {
        AppStateManager.ClosePdfViewer();
        StateHasChanged();
    }
}
