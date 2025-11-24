namespace AzureAIStreamer.Console;

using System;
using System.Threading.Tasks;
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;
using System.ClientModel;
using AzureAIStreamer.Models;

public class ChatService
{
    private readonly ChatClient _chatClient;

    public ChatService(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> GetAIResponseAsync(Conversation conversation, string deploymentName)
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
            _chatClient.CompleteChatStreamingAsync(conversation.Messages, completionOptions);

        var aiResponse = string.Empty;
        var finishTime = string.Empty;
        ChatFinishReason? finishReason = null;

        Console.Write("[AI]: ");

        // Spinner animation
        var spinnerChars = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var spinnerIndex = 0;
        var hasContent = false;
        var spinnerCancellation = new System.Threading.CancellationTokenSource();
        var cursorLeft = Console.CursorLeft;
        var cursorTop = Console.CursorTop;

        // Start spinner in background
        var spinnerTask = Task.Run(async () =>
        {
            try
            {
                while (!spinnerCancellation.Token.IsCancellationRequested)
                {
                    Console.SetCursorPosition(cursorLeft, cursorTop);
                    Console.Write(spinnerChars[spinnerIndex]);
                    spinnerIndex = (spinnerIndex + 1) % spinnerChars.Length;
                    await Task.Delay(100, spinnerCancellation.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        }, spinnerCancellation.Token);

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
                // Stop spinner on first content
                if (!hasContent)
                {
                    hasContent = true;
                    spinnerCancellation.Cancel();
                    try { await spinnerTask; } catch { }
                    Console.SetCursorPosition(cursorLeft, cursorTop);
                    Console.Write(" "); // Clear spinner character
                    Console.SetCursorPosition(cursorLeft, cursorTop);
                }

                aiResponse += contentPart.Text;
                Console.Write(contentPart.Text);
            }

            finishTime = Convert.ToString(completionUpdate.CreatedAt);
        }

        // Ensure spinner is stopped
        if (!hasContent)
        {
            spinnerCancellation.Cancel();
            try { await spinnerTask; } catch { }
            Console.SetCursorPosition(cursorLeft, cursorTop);
            Console.Write(" "); // Clear spinner character
            Console.SetCursorPosition(cursorLeft, cursorTop);
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"\n[AI General: At {finishTime} | Finish reason {finishReason} ]");
        Console.ResetColor();

        Console.WriteLine();
        return aiResponse;
    }
}
