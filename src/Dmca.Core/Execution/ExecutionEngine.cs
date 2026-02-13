using Dmca.Core.Execution.Actions;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dmca.Core.Execution;

/// <summary>
/// Orchestrates execution of an action queue in order.
/// - Runs actions sequentially.
/// - If the restore-point action fails, all subsequent destructive actions are cancelled.
/// - Supports cancellation between actions (not mid-action).
/// - Updates action statuses and overall queue status as it progresses.
/// </summary>
public sealed class ExecutionEngine
{
    private readonly IActionQueueRepository _queueRepo;
    private readonly AuditLogger _auditLogger;
    private readonly Dictionary<ActionType, IActionHandler> _handlers;
    private readonly ILogger<ExecutionEngine> _logger;

    public ExecutionEngine(
        IActionQueueRepository queueRepo,
        AuditLogger auditLogger,
        IEnumerable<IActionHandler> handlers,
        ILogger<ExecutionEngine>? logger = null)
    {
        _queueRepo = queueRepo;
        _auditLogger = auditLogger;
        _handlers = handlers.ToDictionary(h => h.HandledType);
        _logger = logger ?? NullLogger<ExecutionEngine>.Instance;
    }

    /// <summary>
    /// Raised before each action starts, providing the current action and progress info.
    /// </summary>
    public event EventHandler<ActionProgressEventArgs>? ActionStarting;

    /// <summary>
    /// Raised after each action completes.
    /// </summary>
    public event EventHandler<ActionProgressEventArgs>? ActionCompleted;

    /// <summary>
    /// Executes all actions in the queue.
    /// </summary>
    /// <param name="queueId">The queue to execute.</param>
    /// <param name="cancellationToken">Cancellation token — checked between actions.</param>
    /// <returns>The final queue state.</returns>
    public async Task<ActionQueue> ExecuteAsync(Guid queueId, CancellationToken cancellationToken = default)
    {
        using var _ = DmcaLog.BeginTimedOperation(_logger, "ExecutionEngine.ExecuteAsync");

        var queue = await _queueRepo.GetByIdAsync(queueId)
            ?? throw new InvalidOperationException($"Action queue {queueId} not found.");

        if (queue.OverallStatus != ActionStatus.PENDING)
            throw new InvalidOperationException($"Queue {queueId} is not in PENDING status (current: {queue.OverallStatus}).");

        _logger.LogInformation(DmcaLog.Events.ExecutionStarted,
            "Starting execution of queue {QueueId} with {ActionCount} actions in {Mode} mode",
            queueId, queue.Actions.Count, queue.Mode);

        await _queueRepo.UpdateQueueStatusAsync(queueId, ActionStatus.RUNNING);
        queue.OverallStatus = ActionStatus.RUNNING;

        var orderedActions = queue.Actions.OrderBy(a => a.Order).ToList();
        var restorePointFailed = false;
        var anyFailed = false;

        for (int i = 0; i < orderedActions.Count; i++)
        {
            var action = orderedActions[i];

            // Check cancellation between actions
            if (cancellationToken.IsCancellationRequested)
            {
                await CancelRemainingAsync(queue.SessionId, orderedActions, i);
                await _queueRepo.UpdateQueueStatusAsync(queueId, ActionStatus.CANCELLED);
                queue.OverallStatus = ActionStatus.CANCELLED;
                return queue;
            }

            // If restore point failed, cancel all destructive actions
            if (restorePointFailed && action.ActionType != ActionType.CREATE_RESTORE_POINT)
            {
                action.Status = ActionStatus.CANCELLED;
                await _queueRepo.UpdateActionAsync(action);
                await _auditLogger.LogCancelledAsync(queue.SessionId, action);
                continue;
            }

            // Fire starting event
            ActionStarting?.Invoke(this, new ActionProgressEventArgs(action, i + 1, orderedActions.Count));

            // Find handler
            if (!_handlers.TryGetValue(action.ActionType, out var handler))
            {
                action.Status = ActionStatus.FAILED;
                action.ErrorMessage = $"No handler registered for action type {action.ActionType}.";
                action.CompletedAt = DateTime.UtcNow;
                await _queueRepo.UpdateActionAsync(action);
                await _auditLogger.LogFailedAsync(queue.SessionId, action, action.ErrorMessage);
                anyFailed = true;

                ActionCompleted?.Invoke(this, new ActionProgressEventArgs(action, i + 1, orderedActions.Count));
                continue;
            }

            // Execute
            action.Status = ActionStatus.RUNNING;
            action.StartedAt = DateTime.UtcNow;
            await _queueRepo.UpdateActionAsync(action);
            await _auditLogger.LogStartAsync(queue.SessionId, action);

            var result = await handler.ExecuteAsync(action, queue.Mode, cancellationToken);

            action.Command = result.Command;
            action.Output = result.Output;
            action.CompletedAt = DateTime.UtcNow;

            if (queue.Mode == ExecutionMode.DRY_RUN)
            {
                action.Status = ActionStatus.DRY_RUN;
                await _queueRepo.UpdateActionAsync(action);
                await _auditLogger.LogDryRunAsync(queue.SessionId, action, result.Command);
            }
            else if (result.Success)
            {
                action.Status = ActionStatus.COMPLETED;
                await _queueRepo.UpdateActionAsync(action);
                await _auditLogger.LogCompletedAsync(queue.SessionId, action, result.Output);
            }
            else
            {
                action.Status = ActionStatus.FAILED;
                action.ErrorMessage = result.ErrorMessage;
                await _queueRepo.UpdateActionAsync(action);
                await _auditLogger.LogFailedAsync(queue.SessionId, action, result.ErrorMessage ?? "Unknown error", result.Output);
                anyFailed = true;

                // If restore point failed, gate subsequent actions
                if (action.ActionType == ActionType.CREATE_RESTORE_POINT)
                {
                    restorePointFailed = true;
                }
            }

            ActionCompleted?.Invoke(this, new ActionProgressEventArgs(action, i + 1, orderedActions.Count));
        }

        // Determine overall status
        var finalStatus = anyFailed ? ActionStatus.FAILED : ActionStatus.COMPLETED;
        if (queue.Mode == ExecutionMode.DRY_RUN && !anyFailed)
            finalStatus = ActionStatus.DRY_RUN;

        await _queueRepo.UpdateQueueStatusAsync(queueId, finalStatus);
        queue.OverallStatus = finalStatus;

        _logger.LogInformation(DmcaLog.Events.ExecutionCompleted,
            "Execution of queue {QueueId} completed with status {Status}", queueId, finalStatus);

        return queue;
    }

    private async Task CancelRemainingAsync(Guid sessionId, List<ExecutionAction> actions, int startIndex)
    {
        for (int i = startIndex; i < actions.Count; i++)
        {
            var action = actions[i];
            if (action.Status is ActionStatus.PENDING or ActionStatus.RUNNING)
            {
                action.Status = ActionStatus.CANCELLED;
                await _queueRepo.UpdateActionAsync(action);
                await _auditLogger.LogCancelledAsync(sessionId, action);
            }
        }
    }
}

/// <summary>
/// Event arguments for action progress reporting.
/// </summary>
public sealed class ActionProgressEventArgs : EventArgs
{
    public ExecutionAction Action { get; }
    public int Current { get; }
    public int Total { get; }

    public ActionProgressEventArgs(ExecutionAction action, int current, int total)
    {
        Action = action;
        Current = current;
        Total = total;
    }
}
