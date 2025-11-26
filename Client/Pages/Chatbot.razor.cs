using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PolicyChatbot.Shared.Models;
using System;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace PolicyChatbot.Client.Pages;

public partial class Chatbot : IDisposable
{
    [Inject]
    public required IJSRuntime JS { get; set; }

    [Inject]
    public required IAppStateManager AppStateManager { get; set; }
    
    [Inject]
    public required HttpClient Http { get; set; }

    private string userInput = "";
    private bool isLoading = false;
    private DotNetObjectReference<Chatbot>? objRef;
    private bool disposedValue;

    protected override async Task OnInitializedAsync()
    {
        AppStateManager.OnProductSelectedAsync += RefreshAsync;
        AppStateManager.OnCitationSelected += RefreshAsync;
    }

    private void RefreshAsync(object? sender, EventArgs e) => StateHasChanged();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            objRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("registerCitationClickHandler", objRef);
        }
        await JS.InvokeVoidAsync("scrollToBottomById", "scrollContainer");
    }

    [JSInvokable]
    public void OnViewSourceClick(int pageNumber, int citationIndex, string messageId)
    {
        // Find the specific message and its citation
        var message = AppStateManager.ChatMessages.Find(m => m.Id == messageId);
        Citation? citation = message?.Citations.Find(c => c.CitationIndex == citationIndex);

        if (citation != null)
        {
            AppStateManager.ShowCitation(citation);
            StateHasChanged();
        }
        else
        {
            // Fallback: create a basic citation with just the page number
            AppStateManager.ShowCitation(new Citation 
            { 
                PageNumber = pageNumber, 
                CitationIndex = citationIndex,
                HighlightText = "",
                ExtractedText = ""
            });

            StateHasChanged();
        }
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

    private static string RenderMessageWithInlineCitations(ChatMessage message)
    {
        var html = Markdown.ToHtml(message.Content);

        // Replace citation markers with inline citation blocks that include the quoted text
        var citationPattern = new Regex(@"<cite data-citation=""(\d+)"">\[(\d+)\]</cite>");
        html = citationPattern.Replace(html, match =>
        {
            var indexStr = match.Groups[1].Value;
            if (int.TryParse(indexStr, out int index))
            {
                var citation = message.Citations.Find(c => c.CitationIndex == index);
                if (citation != null)
                {
                    // Create compact inline citation with message ID for unique lookup
                    return $@"<span class=""inline-citation""><span class=""citation-marker"">[{index}]</span> <span class=""citation-quote""><span class=""quote-text"">""{EscapeHtml(citation.HighlightText)}""</span> <span class=""view-source-link"" data-page=""{citation.PageNumber}"" data-citation-index=""{index}"" data-message-id=""{message.Id}""><span class=""page-ref"">Page {citation.PageNumber}</span><svg xmlns=""http://www.w3.org/2000/svg"" width=""10"" height=""10"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2""><path d=""M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6""/><polyline points=""15 3 21 3 21 9""/><line x1=""10"" y1=""14"" x2=""21"" y2=""3""/></svg></span></span></span>";
                }
            }
            return match.Value;
        });

        return html;
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private void ClosePdfViewer()
    {
        AppStateManager.ClosePdfViewer();
        StateHasChanged();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                AppStateManager.OnProductSelectedAsync -= RefreshAsync;
                AppStateManager.OnCitationSelected -= RefreshAsync;
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
}