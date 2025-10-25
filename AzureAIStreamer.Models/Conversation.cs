namespace AzureAIStreamer.Models;

using System.Collections.Generic;
using OpenAI.Chat;

public class Conversation
{
    private List<ChatMessage> _messages;

    public Conversation(string systemPrompt)
    {
        _messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt)
        };
    }

    public IEnumerable<ChatMessage> Messages => _messages;

    public void AddUserMessage(string message)
    {
        _messages.Add(new UserChatMessage(message));
    }

    public void AddAssistantMessage(string message)
    {
        _messages.Add(new AssistantChatMessage( message));
    }
}