using Dmca.Core.Models;

namespace Dmca.Core.Interfaces;

/// <summary>
/// Repository for ActionQueue persistence.
/// </summary>
public interface IActionQueueRepository
{
    Task CreateAsync(ActionQueue queue);
    Task<ActionQueue?> GetByIdAsync(Guid id);
    Task<ActionQueue?> GetBySessionIdAsync(Guid sessionId);
    Task UpdateActionAsync(ExecutionAction action);
    Task UpdateQueueStatusAsync(Guid queueId, ActionStatus status);
}
