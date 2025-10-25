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

        // Get configuration values
        var endpointString = configuration["AzureOpenAI:Endpoint"];
        var apiKeyString = configuration["AzureOpenAI:ApiKey"];
        var deploymentName = configuration["AzureOpenAI:DeploymentName"];
        var systemPrompt = configuration["AzureOpenAI:SystemPrompt"];

        // Validate configuration
        if (string.IsNullOrWhiteSpace(endpointString) || string.IsNullOrWhiteSpace(apiKeyString) ||
            string.IsNullOrWhiteSpace(deploymentName) || string.IsNullOrWhiteSpace(systemPrompt))
        {
            Console.WriteLine("Azure OpenAI configuration is missing. Ensure Endpoint, ApiKey, SystemPrompt and DeploymentName are set in appsettings.json or user secrets.");
            return;
        }

        var endpoint = new Uri(endpointString);
        var apiKey = new AzureKeyCredential(apiKeyString);

        // Initialize Azure OpenAI client
        var chatClient = new AzureOpenAIClient(endpoint, apiKey).GetChatClient(deploymentName);

        // Initiate chat history
        Conversation conversation = new Conversation(systemPrompt);
        Console.WriteLine($"You are chatting with an AI model. Using {deploymentName} on {endpointString}.\n\nType '/exit' or press [Enter] to quit.");

        while (true)
        {
            Console.Write("\n[You]: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                break;

            try
            {
                // Add user message to history
                conversation.AddUserMessage(userInput);

                // Get AI response
                string aiResponse = await GetAIResponse(conversation, chatClient, deploymentName);

                // Add AI response to history
                conversation.AddAssistantMessage(aiResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nError while getting AI model response: {0}", ex.Message);
            }
        }
        Console.WriteLine("Goodbye.");
    }

    private static async Task<string> GetAIResponse(Conversation conversation, ChatClient client, string deploymentName)
    {
        var completionOptions = new ChatCompletionOptions
        {
            
            Temperature = 1f,
            MaxOutputTokenCount = 16384,
#pragma warning disable OPENAI001
            ReasoningEffortLevel = ChatReasoningEffortLevel.High
#pragma warning restore OPENAI001
        };

        // The SetNewMaxCompletionTokensPropertyEnabled() method is an [Experimental] opt-in to use
        // the new max_completion_tokens JSON property instead of the legacy max_tokens property.
        // This extension method will be removed and unnecessary in a future service API version;
        // please disable the [Experimental] warning to acknowledge.
#pragma warning disable AOAI001
        completionOptions.SetNewMaxCompletionTokensPropertyEnabled(true);
#pragma warning restore AOAI001

        // Streaming the response
        AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates =
        client.CompleteChatStreamingAsync(conversation.Messages, completionOptions);

        var aiReponse = String.Empty;
        var finishTime = String.Empty;
        ChatFinishReason? finishReason = null;
        
        Console.Write("[AI]: ");

        await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
        {
            if (completionUpdate.FinishReason != null) finishReason = completionUpdate.FinishReason;
            ChatTokenUsage usage = completionUpdate.Usage;
            if (usage != null)
            {
#pragma warning disable OPENAI001
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\n\n[AI Tokens: Total {usage.TotalTokenCount} | Input {usage.InputTokenCount} | Output {usage.OutputTokenCount} | Input cached {usage.InputTokenDetails.CachedTokenCount}]\n[AI Token Details: Output accepted {usage.OutputTokenDetails.AcceptedPredictionTokenCount} | Output rejected {usage.OutputTokenDetails.RejectedPredictionTokenCount} | Output reasoning {usage.OutputTokenDetails.ReasoningTokenCount}]");
                Console.ResetColor();
#pragma warning restore OPENAI001
            }

            foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
            {
                aiReponse += contentPart.Text;
                Console.Write(contentPart.Text);
            }

            finishTime = Convert.ToString(completionUpdate.CreatedAt);
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"\n[AI General: At {finishTime} | Finish reason {finishReason} ]");
        Console.ResetColor();

        Console.WriteLine();
        return aiReponse;
    }
}