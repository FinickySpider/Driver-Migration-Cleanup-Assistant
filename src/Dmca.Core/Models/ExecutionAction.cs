namespace Dmca.Core.Models;

/// <summary>
/// A single executable action within an action queue.
/// </summary>
public sealed class ExecutionAction
{
    public required Guid Id { get; init; }
    public required int Order { get; init; }
    public required ActionType ActionType { get; init; }
    public required string TargetId { get; init; }
    public required string DisplayName { get; init; }
    public ActionStatus Status { get; set; } = ActionStatus.PENDING;
    public string? Command { get; set; }
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
