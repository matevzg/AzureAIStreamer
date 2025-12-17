namespace AzureAIStreamer.Console;

using System;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;

using System.ClientModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using AzureAIStreamer.Models;
using System.Threading;

public class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>()
            .Build();

        // Bind configuration to AppConfig
        var config = new AppConfig
        {
            Endpoint = configuration["AzureOpenAI:Endpoint"] ?? string.Empty,
            ApiKey = configuration["AzureOpenAI:ApiKey"] ?? string.Empty,
            DeploymentName = configuration["AzureOpenAI:DeploymentName"] ?? string.Empty,
            SystemPrompt = configuration["AzureOpenAI:SystemPrompt"] ?? string.Empty
        };

        // Validate configuration
        if (!config.IsValid)
        {
            Console.WriteLine("Azure OpenAI configuration is missing. Ensure Endpoint, ApiKey, SystemPrompt and DeploymentName are set in appsettings.json or user secrets.");
            return;
        }

        var endpoint = new Uri(config.Endpoint);
        var apiKey = new AzureKeyCredential(config.ApiKey);

        // Initialize Azure OpenAI client
        var chatClient = new AzureOpenAIClient(endpoint, apiKey).GetChatClient(config.DeploymentName);

        // Validate API key early to fail fast on misconfiguration
        if (!await ValidateApiKeyAsync(chatClient))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid or missing Azure OpenAI API key. Please check your user secrets or configuration.");
            Console.ResetColor();
            return;
        }

        var chatService = new ChatService(chatClient);

        // Initiate chat history
        Conversation conversation = new Conversation(config.SystemPrompt);
        
        // Display welcome message
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          Azure AI Streamer - Interactive Chat with Token Output           ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine($"\nDeployment name: {config.DeploymentName}");
        Console.WriteLine($"Endpoint: {config.Endpoint}");
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  /exit  - Exit the application");
        Console.WriteLine("  /reset - Reset conversation history\n");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("[You]: ");
            Console.ResetColor();
            var userInput = Console.ReadLine();

            if (userInput?.Equals("/exit", StringComparison.OrdinalIgnoreCase) == true)
                break;

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Equals("/reset", StringComparison.OrdinalIgnoreCase))
            {
                conversation = new Conversation(config.SystemPrompt);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n✓ Conversation history has been reset.\n");
                Console.ResetColor();
                continue;
            }

            try
            {
                // Add user message to history
                conversation.AddUserMessage(userInput);

                // Get AI response
                string aiResponse = await chatService.GetAIResponseAsync(conversation, config.DeploymentName);

                // Add AI response to history
                conversation.AddAssistantMessage(aiResponse);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n✗ Error while getting AI model response: {0}", ex.Message);
                Console.ResetColor();
            }
        }
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\nGoodbye! 👋");
        Console.ResetColor();
    }

    private static async Task<bool> ValidateApiKeyAsync(ChatClient chatClient)
    {
        try
        {
            var completionOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1
            };

            // The SetNewMaxCompletionTokensPropertyEnabled() method is an [Experimental] opt-in to use
            // the new max_completion_tokens JSON property instead of the legacy max_tokens property.
            // This extension method will be removed and unnecessary in a future service API version;
            // please disable the [Experimental] warning to acknowledge.
            #pragma warning disable AOAI001
            completionOptions.SetNewMaxCompletionTokensPropertyEnabled(true);
            #pragma warning restore AOAI001

            var messages = new System.Collections.Generic.List<ChatMessage>
            {
                new SystemChatMessage("Health check: verify API key"),
                new UserChatMessage("Ping. Don't respond.")
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

            await foreach (var _ in chatClient.CompleteChatStreamingAsync(messages, completionOptions, cts.Token))
            {
                // If we receive any update, the key is valid (or at least accepted)
                return true;
            }

            // If streaming completed with no updates, treat as failure
            return false;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            return false;
        }
        catch
        {
            // Any other exception we surface as a validation failure so program doesn't continue with bad config
            return false;
        }
    }
}

public class AppConfig
{
    public required string Endpoint { get; set; }
    public required string ApiKey { get; set; }
    public required string DeploymentName { get; set; }
    public required string SystemPrompt { get; set; }
    
    public bool IsValid => !string.IsNullOrWhiteSpace(Endpoint) &&
                           !string.IsNullOrWhiteSpace(ApiKey) &&
                           !string.IsNullOrWhiteSpace(DeploymentName) &&
                           !string.IsNullOrWhiteSpace(SystemPrompt);
}