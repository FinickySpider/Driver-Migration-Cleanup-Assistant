namespace Dmca.Core.Models;

/// <summary>
/// A single audit log entry capturing an action execution event.
/// </summary>
public sealed class AuditLogEntry
{
    public required Guid Id { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid ActionId { get; init; }
    public required ActionType ActionType { get; init; }
    public required string TargetId { get; init; }
    public required ActionStatus Status { get; init; }
    public required DateTime Timestamp { get; init; }
    public string? Output { get; init; }
    public string? ErrorMessage { get; init; }
}
