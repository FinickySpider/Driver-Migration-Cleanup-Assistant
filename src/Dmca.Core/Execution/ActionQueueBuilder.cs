using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Core.Execution;

/// <summary>
/// Builds an ordered action queue from the decision plan.
/// Only items with REMOVE_STAGE_1 or REMOVE_STAGE_2 are included.
/// BLOCKED, KEEP, and unconfirmed REVIEW items are excluded.
/// CREATE_RESTORE_POINT is always the first action.
/// </summary>
public sealed class ActionQueueBuilder
{
    private readonly IPlanRepository _planRepo;
    private readonly IActionQueueRepository _queueRepo;

    public ActionQueueBuilder(IPlanRepository planRepo, IActionQueueRepository queueRepo)
    {
        _planRepo = planRepo;
        _queueRepo = queueRepo;
    }

    /// <summary>
    /// Builds an action queue from the current plan.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="mode">Execution mode (LIVE or DRY_RUN).</param>
    /// <param name="confirmedReviewItemIds">
    /// Optional set of REVIEW item IDs the user has explicitly confirmed for removal.
    /// </param>
    public async Task<ActionQueue> BuildAsync(
        Guid sessionId,
        ExecutionMode mode = ExecutionMode.LIVE,
        IReadOnlySet<string>? confirmedReviewItemIds = null)
    {
        var plan = await _planRepo.GetCurrentBySessionIdAsync(sessionId)
            ?? throw new InvalidOperationException($"No current plan for session {sessionId}.");

        var actions = new List<ExecutionAction>();
        var order = 0;

        // First action is always CREATE_RESTORE_POINT
        actions.Add(new ExecutionAction
        {
            Id = Guid.NewGuid(),
            Order = order++,
            ActionType = ActionType.CREATE_RESTORE_POINT,
            TargetId = $"session:{sessionId}",
            DisplayName = "Create system restore point",
        });

        // Build removal actions from plan items
        foreach (var item in plan.Items)
        {
            if (item.Recommendation is Recommendation.BLOCKED or Recommendation.KEEP)
                continue;

            if (item.Recommendation == Recommendation.REVIEW)
            {
                // Only include REVIEW items that are explicitly confirmed
                if (confirmedReviewItemIds is null || !confirmedReviewItemIds.Contains(item.ItemId))
                    continue;
            }

            // REMOVE_STAGE_1 and REMOVE_STAGE_2 are included
            var actionType = InferActionType(item.ItemId);
            if (actionType is null) continue;

            actions.Add(new ExecutionAction
            {
                Id = Guid.NewGuid(),
                Order = order++,
                ActionType = actionType.Value,
                TargetId = item.ItemId,
                DisplayName = $"{actionType.Value}: {item.ItemId}",
            });
        }

        var queue = new ActionQueue
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            Mode = mode,
            OverallStatus = ActionStatus.PENDING,
            Actions = actions,
        };

        await _queueRepo.CreateAsync(queue);
        return queue;
    }

    /// <summary>
    /// Infers the action type from the item ID prefix.
    /// </summary>
    internal static ActionType? InferActionType(string itemId)
    {
        if (itemId.StartsWith("drv:", StringComparison.OrdinalIgnoreCase) ||
            itemId.StartsWith("pkg:", StringComparison.OrdinalIgnoreCase))
            return ActionType.UNINSTALL_DRIVER_PACKAGE;

        if (itemId.StartsWith("svc:", StringComparison.OrdinalIgnoreCase))
            return ActionType.DISABLE_SERVICE;

        if (itemId.StartsWith("app:", StringComparison.OrdinalIgnoreCase))
            return ActionType.UNINSTALL_PROGRAM;

        return null;
    }
}
