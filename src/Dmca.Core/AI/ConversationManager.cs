using System.Text.Json;

namespace Dmca.Core.AI;

/// <summary>
/// Represents a single message in an OpenAI chat conversation.
/// </summary>
public sealed class ChatMessage
{
    public required string Role { get; init; }
    public string? Content { get; init; }
    public IReadOnlyList<ToolCallMessage>? ToolCalls { get; init; }
    public string? ToolCallId { get; init; }
    public string? Name { get; init; }
}

/// <summary>
/// A tool call within an assistant message.
/// </summary>
public sealed class ToolCallMessage
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required ToolCallFunction Function { get; init; }
}

/// <summary>
/// Function details within a tool call.
/// </summary>
public sealed class ToolCallFunction
{
    public required string Name { get; init; }
    public required string Arguments { get; init; }
}

/// <summary>
/// Manages conversation state for an AI advisor session.
/// Tracks messages, enforces system prompt, and validates tool calls.
/// </summary>
public sealed class ConversationManager
{
    private readonly List<ChatMessage> _messages = [];
    private readonly string _systemPrompt;

    public ConversationManager(string systemPrompt)
    {
        _systemPrompt = systemPrompt;
        _messages.Add(new ChatMessage { Role = "system", Content = _systemPrompt });
    }

    /// <summary>
    /// All messages in the conversation (system + user + assistant + tool results).
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Adds a user message.
    /// </summary>
    public void AddUserMessage(string content)
    {
        _messages.Add(new ChatMessage { Role = "user", Content = content });
    }

    /// <summary>
    /// Adds an assistant message (text response).
    /// </summary>
    public void AddAssistantMessage(string content)
    {
        _messages.Add(new ChatMessage { Role = "assistant", Content = content });
    }

    /// <summary>
    /// Adds an assistant message that contains tool calls.
    /// </summary>
    public void AddAssistantToolCallMessage(IReadOnlyList<ToolCallMessage> toolCalls)
    {
        _messages.Add(new ChatMessage
        {
            Role = "assistant",
            ToolCalls = toolCalls,
        });
    }

    /// <summary>
    /// Adds the result of a tool call.
    /// </summary>
    public void AddToolResult(string toolCallId, string result)
    {
        _messages.Add(new ChatMessage
        {
            Role = "tool",
            Content = result,
            ToolCallId = toolCallId,
        });
    }

    /// <summary>
    /// Resets the conversation (keeps system prompt).
    /// </summary>
    public void Reset()
    {
        _messages.Clear();
        _messages.Add(new ChatMessage { Role = "system", Content = _systemPrompt });
    }

    /// <summary>
    /// Serializes messages to a JSON array compatible with the OpenAI API.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(_messages, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        });
    }
}
