using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Core.Execution;

/// <summary>
/// Audit logger that records every action execution event.
/// Every action produces at least two entries: start and end (success/failure/skip).
/// </summary>
public sealed class AuditLogger
{
    private readonly IAuditLogRepository _repo;

    public AuditLogger(IAuditLogRepository repo) => _repo = repo;

    /// <summary>
    /// Logs an action starting.
    /// </summary>
    public async Task LogStartAsync(Guid sessionId, ExecutionAction action)
    {
        await _repo.CreateAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ActionId = action.Id,
            ActionType = action.ActionType,
            TargetId = action.TargetId,
            Status = ActionStatus.RUNNING,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Logs an action completed successfully.
    /// </summary>
    public async Task LogCompletedAsync(Guid sessionId, ExecutionAction action, string? output = null)
    {
        await _repo.CreateAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ActionId = action.Id,
            ActionType = action.ActionType,
            TargetId = action.TargetId,
            Status = ActionStatus.COMPLETED,
            Timestamp = DateTime.UtcNow,
            Output = output,
        });
    }

    /// <summary>
    /// Logs an action that failed.
    /// </summary>
    public async Task LogFailedAsync(Guid sessionId, ExecutionAction action, string errorMessage, string? output = null)
    {
        await _repo.CreateAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ActionId = action.Id,
            ActionType = action.ActionType,
            TargetId = action.TargetId,
            Status = ActionStatus.FAILED,
            Timestamp = DateTime.UtcNow,
            Output = output,
            ErrorMessage = errorMessage,
        });
    }

    /// <summary>
    /// Logs a dry-run action (skipped).
    /// </summary>
    public async Task LogDryRunAsync(Guid sessionId, ExecutionAction action, string? command = null)
    {
        await _repo.CreateAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ActionId = action.Id,
            ActionType = action.ActionType,
            TargetId = action.TargetId,
            Status = ActionStatus.DRY_RUN,
            Timestamp = DateTime.UtcNow,
            Output = command is not null ? $"[DRY RUN] {command}" : null,
        });
    }

    /// <summary>
    /// Logs a cancelled action.
    /// </summary>
    public async Task LogCancelledAsync(Guid sessionId, ExecutionAction action)
    {
        await _repo.CreateAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ActionId = action.Id,
            ActionType = action.ActionType,
            TargetId = action.TargetId,
            Status = ActionStatus.CANCELLED,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Retrieves all audit entries for a session.
    /// </summary>
    public async Task<IReadOnlyList<AuditLogEntry>> GetBySessionAsync(Guid sessionId) =>
        await _repo.GetBySessionIdAsync(sessionId);
}
