using Dmca.Core.AI;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="ConversationManager"/> message management.
/// </summary>
public sealed class ConversationManagerTests
{
    [Fact]
    public void Constructor_AddsSystemPrompt()
    {
        var convo = new ConversationManager("You are a helpful assistant.");

        Assert.Single(convo.Messages);
        Assert.Equal("system", convo.Messages[0].Role);
        Assert.Equal("You are a helpful assistant.", convo.Messages[0].Content);
    }

    [Fact]
    public void AddUserMessage_AppendsToHistory()
    {
        var convo = new ConversationManager("System prompt");
        convo.AddUserMessage("Hello");

        Assert.Equal(2, convo.Messages.Count);
        Assert.Equal("user", convo.Messages[1].Role);
        Assert.Equal("Hello", convo.Messages[1].Content);
    }

    [Fact]
    public void AddAssistantMessage_AppendsToHistory()
    {
        var convo = new ConversationManager("System prompt");
        convo.AddAssistantMessage("Hi there!");

        Assert.Equal(2, convo.Messages.Count);
        Assert.Equal("assistant", convo.Messages[1].Role);
    }

    [Fact]
    public void AddToolResult_AppendsWithId()
    {
        var convo = new ConversationManager("System prompt");
        convo.AddToolResult("call-123", "{\"data\":\"value\"}");

        Assert.Equal(2, convo.Messages.Count);
        Assert.Equal("tool", convo.Messages[1].Role);
        Assert.Equal("call-123", convo.Messages[1].ToolCallId);
    }

    [Fact]
    public void Reset_KeepsOnlySystemPrompt()
    {
        var convo = new ConversationManager("System prompt");
        convo.AddUserMessage("msg1");
        convo.AddAssistantMessage("msg2");

        convo.Reset();

        Assert.Single(convo.Messages);
        Assert.Equal("system", convo.Messages[0].Role);
    }

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var convo = new ConversationManager("System prompt");
        convo.AddUserMessage("Hello");

        var json = convo.ToJson();

        Assert.Contains("system", json);
        Assert.Contains("Hello", json);
        Assert.StartsWith("[", json);
    }

    [Fact]
    public void AddAssistantToolCallMessage_StoresToolCalls()
    {
        var convo = new ConversationManager("System prompt");
        var toolCalls = new List<ToolCallMessage>
        {
            new()
            {
                Id = "call-1",
                Type = "function",
                Function = new ToolCallFunction
                {
                    Name = "get_session",
                    Arguments = "{}",
                },
            },
        };

        convo.AddAssistantToolCallMessage(toolCalls);

        Assert.Equal(2, convo.Messages.Count);
        Assert.Equal("assistant", convo.Messages[1].Role);
        Assert.NotNull(convo.Messages[1].ToolCalls);
        Assert.Single(convo.Messages[1].ToolCalls!);
    }
}
