namespace Dmca.Core.Models;

/// <summary>
/// An ordered queue of actions to execute against the system.
/// </summary>
public sealed class ActionQueue
{
    public required Guid Id { get; init; }
    public required Guid SessionId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required ExecutionMode Mode { get; init; }
    public ActionStatus OverallStatus { get; set; } = ActionStatus.PENDING;
    public List<ExecutionAction> Actions { get; init; } = [];
}
