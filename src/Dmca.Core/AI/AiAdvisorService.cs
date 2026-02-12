namespace Dmca.Core.AI;

/// <summary>
/// Orchestrates the AI advisor conversation loop.
/// Sends user messages, processes tool calls, enforces safety guardrails,
/// and returns the final assistant text.
/// </summary>
public sealed class AiAdvisorService
{
    private const int MaxToolCallRounds = 10; // prevent infinite loops

    private readonly IAiModelClient _modelClient;
    private readonly ToolDispatcher _toolDispatcher;
    private readonly ConversationManager _conversation;

    public AiAdvisorService(
        IAiModelClient modelClient,
        ToolDispatcher toolDispatcher,
        string systemPrompt)
    {
        _modelClient = modelClient;
        _toolDispatcher = toolDispatcher;
        _conversation = new ConversationManager(systemPrompt);
    }

    /// <summary>
    /// Sends a user message to the AI advisor and returns the final assistant response.
    /// Automatically handles multi-round tool calling.
    /// </summary>
    public async Task<AiAdvisorResult> ChatAsync(
        string userMessage,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _conversation.AddUserMessage(userMessage);

        var toolCallsExecuted = new List<ToolCallRecord>();
        var safetyViolations = new List<string>();

        for (var round = 0; round < MaxToolCallRounds; round++)
        {
            var response = await _modelClient.SendAsync(_conversation, cancellationToken);

            // Check for forbidden phrases in text content
            if (response.Content is not null)
            {
                var violations = AiSafetyGuard.DetectForbiddenPhrases(response.Content);
                if (violations.Count > 0)
                {
                    safetyViolations.AddRange(violations);
                    // Still return the response but flag the violations
                    _conversation.AddAssistantMessage(response.Content);
                    return new AiAdvisorResult
                    {
                        Content = response.Content,
                        ToolCallsExecuted = toolCallsExecuted.AsReadOnly(),
                        SafetyViolations = safetyViolations.AsReadOnly(),
                        Blocked = true,
                    };
                }
            }

            if (!response.HasToolCalls)
            {
                // Final text response
                var content = response.Content ?? "";
                _conversation.AddAssistantMessage(content);
                return new AiAdvisorResult
                {
                    Content = content,
                    ToolCallsExecuted = toolCallsExecuted.AsReadOnly(),
                    SafetyViolations = safetyViolations.AsReadOnly(),
                    Blocked = false,
                };
            }

            // Process tool calls
            _conversation.AddAssistantToolCallMessage(response.ToolCalls);

            foreach (var toolCall in response.ToolCalls)
            {
                var toolName = toolCall.Function.Name;
                var toolArgs = toolCall.Function.Arguments;

                string result;
                if (!AiSafetyGuard.IsAllowedTool(toolName))
                {
                    result = $"{{\"error\": \"Tool '{toolName}' is not allowed.\"}}";
                    safetyViolations.Add($"AI attempted to call disallowed tool: {toolName}");
                }
                else
                {
                    result = await _toolDispatcher.DispatchAsync(toolName, toolArgs, sessionId);
                }

                toolCallsExecuted.Add(new ToolCallRecord(toolCall.Id, toolName, toolArgs, result));
                _conversation.AddToolResult(toolCall.Id, result);
            }
        }

        // Max rounds exceeded
        return new AiAdvisorResult
        {
            Content = "I've reached the maximum number of tool call rounds. Please try again with a simpler request.",
            ToolCallsExecuted = toolCallsExecuted.AsReadOnly(),
            SafetyViolations = ["Max tool call rounds exceeded."],
            Blocked = true,
        };
    }

    /// <summary>
    /// Resets the conversation history.
    /// </summary>
    public void ResetConversation() => _conversation.Reset();
}

/// <summary>
/// Result of an AI advisor chat interaction.
/// </summary>
public sealed class AiAdvisorResult
{
    public required string Content { get; init; }
    public required IReadOnlyList<ToolCallRecord> ToolCallsExecuted { get; init; }
    public required IReadOnlyList<string> SafetyViolations { get; init; }
    public required bool Blocked { get; init; }
}

/// <summary>
/// Record of a tool call executed during the conversation.
/// </summary>
public sealed record ToolCallRecord(
    string ToolCallId,
    string ToolName,
    string Arguments,
    string Result);
