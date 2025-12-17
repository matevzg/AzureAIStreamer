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

## Multi-targeting

This repository now multi-targets `net10.0`, `net9.0`, and `net8.0` for both the console and models projects.

- Build for all target frameworks (default):

```bash
dotnet build
```

- Build for a single target framework:

```bash
dotnet build -f net8.0
dotnet build -f net9.0
dotnet build -f net10.0
```

- Run the console app for a specific framework:

```bash
dotnet run -f net8.0 --project AzureAIStreamer.Console
```

Notes on package compatibility and conditional code:

- The project references `Azure.AI.OpenAI` (2.5.0-beta.1) which targets `.NET 8.0` and `.NET Standard 2.0` (compatible with newer runtimes). The Microsoft configuration packages target `.NET 6.0` / `.NET Standard 2.0` and are compatible with higher frameworks. No package changes were required for `net8/net9/net10`.
- If you need framework-specific package versions or APIs, use conditional `ItemGroup` elements in the `.csproj`:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <PackageReference Include="Some.Package" Version="x.y.z" />
</ItemGroup>
```

- For source-level differences, use preprocessor symbols exposed by SDKs: `NET8_0`, `NET9_0`, `NET10_0`.

If you'd like, I can add example conditional references or guards for any package that needs special handling.

## Usage

1. Configure your Azure OpenAI settings in `appsettings.json`
2. Set your API key in user secrets
3. Run the application
4. Start chatting with the AI model
5. Type '/exit' or press [Enter] to quit

## License

This project is licensed under the MIT License - see the LICENSE file for details.