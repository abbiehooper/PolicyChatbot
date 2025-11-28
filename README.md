# PolicyChatbot

An intelligent chatbot application built with Blazor WebAssembly and .NET 10 that enables users to ask questions about insurance policies. The chatbot leverages AI-powered responses to provide accurate policy information across multiple insurance types and providers.

## Features

- **Multi-tier Insurance Selection**: Filter policies by insurance type, insurer, and specific product.
- **AI-Powered Responses**: Integrates with Anthropic's API for intelligent policy question answering.
- **PDF Policy Support**: Automatically extracts and processes policy documents in PDF format.
- **Real-time Chat Interface**: Interactive chat UI with message history and loading states.
- **Responsive Design**: Built with Bootstrap for a mobile-friendly experience.
- **Error Handling**: Comprehensive error handling with user-friendly messages.

## Tech Stack

- **Frontend**: Blazor WebAssembly with .NET 10
- **Backend**: ASP.NET Core with .NET 10
- **UI Framework**: Bootstrap 5
- **PDF Processing**: UglyToad.PdfPig
- **AI Integration**: Anthropic API (Claude)
- **Architecture**: Client-Server with Razor Pages

## Installation & Setup

### 1. Clone the Repository

git clone https://github.com/abbiehooper/PolicyChatbot.git
cd PolicyChatbot


### 2. Configure Anthropic API

Create or update `appsettings.json` in the `Server` project with your Anthropic credentials:


{
  "Anthropic": {
    "BaseUrl": "https://api.anthropic.com",
    "ApiKey": "your-api-key-here"
  }
}


### 3. Organize Policy Documents

Create the following directory structure in `Server/PolicyDocuments`:
```
PolicyDocuments/
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

### 4. Build and Run

dotnet build
dotnet run --project Server


The application will be available at `https://localhost:5001` (or the configured port).

## Usage

1. **Select Insurance Type**: Choose from available insurance categories.
2. **Select Insurer**: Pick an insurer offering that insurance type.
3. **Select Product**: Choose a specific policy product.
4. **Ask Questions**: Type questions about the selected policy.
5. **Get AI Responses**: Receive intelligent answers powered by Claude.

## API Endpoints

The server exposes the following REST API endpoints:

- `GET /api/policy/insurance-types` - Get available insurance types.
- `GET /api/policy/insurers?insuranceType={type}` - Get insurers for a type.
- `GET /api/policy/products?insuranceType={type}&insurer={name}` - Get products.
- `POST /api/chatbot/chat` - Send a policy question and get AI response.

## Configuration

### Anthropic API Integration

The `AnthropicOptions` class manages API configuration:

- **BaseUrl**: Anthropic API endpoint.
- **ApiKey**: Your Anthropic API key.

Configure via `appsettings.json` or environment variables.

### Dependency Injection

Core services are registered in `DependencyInjection.cs`:

- `IPolicyService` - Manages policy documents and metadata.
- `IChatbotService` - Handles AI chat interactions.

## Architecture Decisions

- **Blazor WebAssembly**: Provides rich, interactive UI without requiring JavaScript.
- **Server-side Policy Management**: Keeps sensitive policy documents secure.
- **PDF Text Extraction**: Automatically processes policy documents for context.
- **Separation of Concerns**: Distinct services for policies and chatbot logic.

## Development

### Building the Project

dotnet build

### Publishing

dotnet publish -c Release -o ./publish

## Troubleshooting

### "Anthropic configuration section is missing or invalid"

Ensure `appsettings.json` contains the correct Anthropic configuration section.

### "PDF file not found"

Verify that policy PDFs are placed in the correct directory structure under `Server/PolicyDocuments`.

### Chat responses not appearing

Check that the Anthropic API key is valid and the API service is accessible.

---


**Built with ❤️ using Blazor WebAssembly and .NET 10**
