# PolicyChatbot

An intelligent AI-powered chatbot application built with Blazor WebAssembly and .NET 10 that enables users to ask natural language questions about insurance policies. The application uses Anthropic's Claude AI to provide accurate, contextual answers with citations directly referencing the source policy documents.

## ✨ Key Features

### 🎯 Smart Policy Navigation
- **Multi-tier Selection**: Filter policies by insurance type (Car, Home, Van, etc.), insurer, and specific product
- **Clean Interface**: Intuitive MudBlazor-based UI for seamless policy selection

### 🤖 AI-Powered Q&A
- **Natural Language Processing**: Ask questions in plain English about complex policy documents
- **Contextual Responses**: Claude maintains conversation history for follow-up questions
- **Source Citations**: Every answer includes inline citations with page numbers and quoted text
- **Split-Screen PDF Viewer**: View the source policy document alongside the chat interface

### 💰 Cost-Optimized Architecture
- **Prompt Caching**: Leverages Claude's prompt caching to reduce API costs by up to 90%
- **Conversation Memory**: Maintains context across questions without re-sending policy documents
- **Rate Limiting**: Built-in middleware to control API usage (10 requests/minute, 50/hour per IP)

### 📄 Advanced PDF Processing
- **Automatic Text Extraction**: Extracts and processes policy documents using PdfPig
- **Page-Level Indexing**: Maintains page numbers for accurate citations
- **Direct PDF Navigation**: Citations link directly to the relevant page in the PDF viewer

## 🏗️ Architecture

### Technology Stack

**Frontend:**
- Blazor WebAssembly (.NET 10)
- MudBlazor UI Components
- Markdig for Markdown rendering
- Custom JavaScript interop for PDF navigation

**Backend:**
- ASP.NET Core (.NET 10)
- Anthropic Claude API (claude-haiku-4-5-20251001)
- PdfPig for PDF text extraction
- Custom rate limiting middleware

**Infrastructure:**
- Singleton services for policy and chatbot management
- In-memory conversation context with automatic cleanup
- File-based policy document storage

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An [Anthropic API key](https://www.anthropic.com/)
- PDF policy documents

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/PolicyChatbot.git
   cd PolicyChatbot
   ```

2. **Configure Anthropic API**
   
   Create `Server/appsettings.json`:
   ```json
   {
     "Anthropic": {
       "BaseUrl": "https://api.anthropic.com",
       "ApiKey": "your-anthropic-api-key-here"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

3. **Organize Policy Documents**

   Create the following directory structure in `Server/PolicyDocuments`:
   ```
   Server/PolicyDocuments/
   ├── Car/
   │   ├── Insurer1/
   │   │   ├── Basic_Coverage.pdf
   │   │   └── Premium_Coverage.pdf
   │   └── Insurer2/
   │       └── Standard_Plan.pdf
   ├── Home/
   │   └── Insurer1/
   │       └── Homeowners_Policy.pdf
   └── Van/
       └── Insurer3/
           └── Commercial_Van.pdf
   ```

   The directory structure determines:
   - **First level**: Insurance type (e.g., Car, Home, Van)
   - **Second level**: Insurer name
   - **Third level**: PDF files (product names derived from filenames)

4. **Build and Run**
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project Server
   ```

5. **Access the Application**
   
   Navigate to `https://localhost:7120` (or the configured HTTPS port)

## 💡 How It Works

### User Flow

1. **Select Policy**: Choose insurance type → insurer → specific product
2. **Ask Questions**: Type natural language questions about the policy
3. **Get AI Answers**: Receive contextual responses with inline citations
4. **View Sources**: Click citation links to view the exact page and quoted text in the PDF

### Under the Hood

#### Prompt Caching Strategy

The chatbot uses Claude's prompt caching feature to optimize costs:

1. **First Request**: 
   - Sends the full policy document marked for caching
   - Claude caches the policy content (ephemeral cache, ~5 minutes)
   - Processes the user's question

2. **Subsequent Requests**:
   - Only sends the user's question and conversation history
   - Claude reuses the cached policy content
   - Dramatically reduces input tokens and costs

3. **Conversation Management**:
   - Maintains up to 20 messages (10 Q&A exchanges) per policy
   - Automatically cleans up conversations after 2 hours of inactivity

#### Citation Processing

Citations are extracted using a custom format:
```
[CITE:page_number:"exact quoted text"]
```

The system:
1. Instructs Claude to use this format when referencing policy text
2. Parses citations from Claude's response
3. Converts them to clickable inline citations with:
   - Citation number badge
   - Quoted text preview
   - "View Source" link with page number
4. Opens the PDF to the exact page when clicked

## 🔌 API Endpoints

### Policy Management

- `GET /api/policy/insurance-types`
  - Returns available insurance types
  
- `GET /api/policy/insurers?insuranceType={type}`
  - Returns insurers offering a specific insurance type
  
- `GET /api/policy/products?insuranceType={type}&insurer={name}`
  - Returns products for a specific insurer and type
  
- `GET /api/policy/pdf/{productId}`
  - Streams the PDF file for a product

### Chatbot

- `POST /api/chatbot/chat`
  - Request body: `{ "productId": "string", "question": "string" }`
  - Returns: `{ "answer": "string", "citations": [...] }`
  
- `POST /api/chatbot/clear-conversation`
  - Request body: `{ "productId": "string" }`
  - Clears conversation history for a product

## ⚙️ Configuration

### Rate Limiting

Configure in `Server/Middleware/RateLimitingMiddleware.cs`:
```csharp
private const int MaxRequestsPerMinute = 10;
private const int MaxRequestsPerHour = 50;
```

### Conversation History

Configure in `Server/Services/ChatbotService.cs`:
```csharp
// Maximum messages to keep (10 exchanges)
if (context.Messages.Count > 20)
{
    context.Messages.RemoveRange(0, context.Messages.Count - 20);
}

// Conversation cleanup time
var cutoffTime = DateTime.UtcNow.AddHours(-2);
```

### Claude Model

Currently using `claude-haiku-4-5-20251001` for cost-effectiveness. To change:
```csharp
// In Server/Services/ChatbotService.cs
model = "claude-sonnet-4-5-20250929"  // For higher quality
```

## 🎨 UI Components

### Main Components

- **`PolicySelection.razor`**: Cascading dropdowns for policy selection
- **`Chatbot.razor`**: Main chat interface with message history
- **`PdfViewer.razor`**: Split-screen PDF viewer with citation highlighting
- **`TypingIndicator.razor`**: Loading animation during AI responses

### State Management

- **`AppStateManager`**: Centralized state management for:
  - Policy selection state
  - Chat messages
  - PDF viewer state
  - Error handling

## 🔐 Security & Best Practices

### Implemented

- ✅ API key stored in configuration (not in code)
- ✅ Rate limiting per IP address
- ✅ Input validation on all endpoints
- ✅ Automatic cleanup of old conversations
- ✅ HTTPS enforcement in production

### Recommended for Production

- [ ] Add authentication/authorization
- [ ] Implement user-specific rate limits
- [ ] Store API key in Azure Key Vault or similar
- [ ] Add request/response logging
- [ ] Implement proper error tracking (e.g., Application Insights)
- [ ] Add unit and integration tests
- [ ] Configure CORS policies
- [ ] Add health check endpoints

## 🐛 Troubleshooting

### "Anthropic configuration section is missing or invalid"
**Solution**: Ensure `Server/appsettings.json` exists with valid Anthropic configuration.

### "PDF file not found"
**Solution**: Verify PDF files are in `Server/PolicyDocuments` with the correct directory structure.

### Chat responses not appearing
**Solutions**:
- Check Anthropic API key validity
- Verify network connectivity to api.anthropic.com
- Check browser console for JavaScript errors

### Rate limit errors
**Solution**: Wait for the time period specified in the error, or adjust rate limits in `RateLimitingMiddleware.cs`.

### Citations not clickable
**Solution**: Ensure JavaScript is enabled and check browser console for errors.

## 📊 Cost Optimization Tips

1. **Use Prompt Caching**: Already implemented - saves ~90% on repeated queries
2. **Choose the Right Model**: Haiku is cost-effective for this use case
3. **Limit Conversation History**: Keep only recent exchanges (already limited to 20 messages)
4. **Monitor Usage**: Check logs for cache hit/miss rates
5. **Set Rate Limits**: Prevent abuse and runaway costs

## 🔮 Future Enhancements

Potential improvements:
- Multi-document comparison queries
- Support for additional document formats (DOCX, TXT)
- User accounts and saved conversations
- Advanced search within policies
- Multi-language support
- Export conversation history
- Mobile app version
- Integration with insurance provider APIs

## 📝 Development

### Project Structure

```
PolicyChatbot/
├── Client/                          # Blazor WebAssembly frontend
│   ├── Components/                  # Reusable UI components
│   ├── Pages/                       # Page components
│   ├── Shared/                      # Shared layouts
│   └── wwwroot/                     # Static assets
├── Server/                          # ASP.NET Core backend
│   ├── Controllers/                 # API controllers
│   ├── Services/                    # Business logic
│   ├── Middleware/                  # Custom middleware
│   ├── Startup/                     # Configuration
│   └── PolicyDocuments/             # PDF storage
└── Shared/                          # Shared models
    └── Models/                      # DTOs and domain models
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

### Running Tests

```bash
dotnet test
```

## 📄 License

This project is provided as-is for educational and commercial use.

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📧 Support

For issues and questions:
- Create an issue on GitHub
- Contact the development team

---

**Built with ❤️ using Blazor WebAssembly, .NET 10, and Anthropic Claude**