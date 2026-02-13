using Dmca.Core.Models;
using Dmca.Core.Services;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="SessionStateMachine"/> transition logic.
/// </summary>
public sealed class SessionStateMachineTests
{
    [Theory]
    [InlineData(SessionStatus.NEW, SessionStatus.SCANNED)]
    [InlineData(SessionStatus.NEW, SessionStatus.FAILED)]
    [InlineData(SessionStatus.SCANNED, SessionStatus.PLANNED)]
    [InlineData(SessionStatus.PLANNED, SessionStatus.PENDING_APPROVALS)]
    [InlineData(SessionStatus.PLANNED, SessionStatus.READY_TO_EXECUTE)]
    [InlineData(SessionStatus.PENDING_APPROVALS, SessionStatus.READY_TO_EXECUTE)]
    [InlineData(SessionStatus.READY_TO_EXECUTE, SessionStatus.EXECUTING)]
    [InlineData(SessionStatus.EXECUTING, SessionStatus.COMPLETED)]
    [InlineData(SessionStatus.EXECUTING, SessionStatus.FAILED)]
    [InlineData(SessionStatus.FAILED, SessionStatus.NEW)]
    public void CanTransition_ValidPairs_ReturnsTrue(SessionStatus from, SessionStatus to)
    {
        Assert.True(SessionStateMachine.CanTransition(from, to));
    }

    [Theory]
    [InlineData(SessionStatus.NEW, SessionStatus.COMPLETED)]
    [InlineData(SessionStatus.COMPLETED, SessionStatus.NEW)]
    [InlineData(SessionStatus.SCANNED, SessionStatus.EXECUTING)]
    [InlineData(SessionStatus.EXECUTING, SessionStatus.NEW)]
    [InlineData(SessionStatus.NEW, SessionStatus.PLANNED)]
    [InlineData(SessionStatus.PLANNED, SessionStatus.COMPLETED)]
    public void CanTransition_InvalidPairs_ReturnsFalse(SessionStatus from, SessionStatus to)
    {
        Assert.False(SessionStateMachine.CanTransition(from, to));
    }

    [Fact]
    public void ValidateTransition_InvalidPair_Throws()
    {
        var ex = Assert.Throws<Core.SessionStateException>(
            () => SessionStateMachine.ValidateTransition(SessionStatus.NEW, SessionStatus.COMPLETED));

        Assert.Contains("Invalid session status transition", ex.Message);
    }

    [Fact]
    public void ValidateTransition_ValidPair_DoesNotThrow()
    {
        var exception = Record.Exception(
            () => SessionStateMachine.ValidateTransition(SessionStatus.NEW, SessionStatus.SCANNED));

        Assert.Null(exception);
    }
}
