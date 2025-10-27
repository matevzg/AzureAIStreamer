# Azure AI Streamer

A .NET console application that demonstrates real-time streaming interactions with Azure OpenAI's chat models. This project showcases how to integrate Azure OpenAI services into a .NET application with proper configuration management and conversation handling.

## Features

- Real-time streaming of AI responses
- Azure OpenAI integration
- Configurable system prompts
- Secure configuration management with user secrets
- Conversation state management
- Clean architecture with separate models project

## Prerequisites

- .NET 9.0 or later
- Azure subscription
- Azure OpenAI service instance
- Visual Studio 2022 or VS Code

## Configuration

The application uses the following configuration structure in `appsettings.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "your-azure-openai-endpoint",
    "DeploymentName": "your-model-deployment-name",
    "SystemPrompt": "your-system-prompt"
  }
}
```

For security reasons, the API key should be stored in user secrets. To set up user secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
```

## Project Structure

- `AzureAIStreamer.Console/` - Main console application
  - `Program.cs` - Application entry point and main logic
  - `appsettings.json` - Configuration file
- `AzureAIStreamer.Models/` - Class library for models
  - `Conversation.cs` - Conversation state management

## Building and Running

To build the project:

```powershell
dotnet build
```

To run the application:

```powershell
dotnet run --project AzureAIStreamer.Console
```

## Usage

1. Configure your Azure OpenAI settings in `appsettings.json`
2. Set your API key in user secrets
3. Run the application
4. Start chatting with the AI model
5. Type '/exit' or press [Enter] to quit

## License

This project is licensed under the MIT License - see the LICENSE file for details.