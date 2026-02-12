namespace Dmca.Core.AI;

/// <summary>
/// Interface for the AI model client (OpenAI or offline).
/// Allows swapping implementations for testing.
/// </summary>
public interface IAiModelClient
{
    /// <summary>
    /// Sends the conversation to the model and returns the assistant response.
    /// The response may contain text content, tool calls, or both.
    /// </summary>
    Task<AiResponse> SendAsync(ConversationManager conversation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from the AI model.
/// </summary>
public sealed class AiResponse
{
    /// <summary>Text content of the response (null if tool-call-only).</summary>
    public string? Content { get; init; }

    /// <summary>Tool calls requested by the model (empty if text-only).</summary>
    public IReadOnlyList<ToolCallMessage> ToolCalls { get; init; } = [];

    /// <summary>The finish reason from the API.</summary>
    public required string FinishReason { get; init; }

    /// <summary>Whether the model wants to call tools.</summary>
    public bool HasToolCalls => ToolCalls.Count > 0;
}
