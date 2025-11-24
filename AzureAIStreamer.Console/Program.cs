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
}