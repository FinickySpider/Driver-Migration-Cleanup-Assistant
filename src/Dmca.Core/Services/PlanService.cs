using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Scoring;

namespace Dmca.Core.Services;

/// <summary>
/// Generates a DecisionPlan by scoring all inventory items and evaluating hard blocks.
/// Persists the plan and transitions the session to PLANNED.
/// </summary>
public sealed class PlanService
{
    private readonly RulesConfig _rules;
    private readonly BaselineScorer _scorer;
    private readonly HardBlockEvaluator _hardBlockEvaluator;
    private readonly IPlanRepository _planRepo;
    private readonly ISnapshotRepository _snapshotRepo;
    private readonly IUserFactRepository _userFactRepo;
    private readonly SessionService _sessionService;

    public PlanService(
        RulesConfig rules,
        IPlanRepository planRepo,
        ISnapshotRepository snapshotRepo,
        IUserFactRepository userFactRepo,
        SessionService sessionService)
    {
        _rules = rules;
        _scorer = new BaselineScorer(rules);
        _hardBlockEvaluator = new HardBlockEvaluator(rules);
        _planRepo = planRepo;
        _snapshotRepo = snapshotRepo;
        _userFactRepo = userFactRepo;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Generates a decision plan for the given session using the latest snapshot.
    /// </summary>
    public async Task<DecisionPlan> GeneratePlanAsync(Guid sessionId)
    {
        var snapshot = await _snapshotRepo.GetLatestBySessionIdAsync(sessionId)
            ?? throw new InvalidOperationException($"No snapshot found for session {sessionId}.");

        var userFacts = await _userFactRepo.GetBySessionIdAsync(sessionId);
        var planItems = new List<PlanItem>();

        foreach (var item in snapshot.Items)
        {
            var scored = _scorer.Score(item, userFacts);
            var hardBlocks = _hardBlockEvaluator.Evaluate(item);

            var recommendation = hardBlocks.Count > 0
                ? Recommendation.BLOCKED
                : Enum.Parse<Recommendation>(scored.Recommendation);

            var finalScore = scored.BaselineScore; // no AI delta yet

            var rationale = scored.EngineRationale.ToList();
            if (hardBlocks.Count > 0)
            {
                rationale.Add($"BLOCKED by: {string.Join(", ", hardBlocks.Select(b => b.Code))}");
            }

            planItems.Add(new PlanItem
            {
                ItemId = item.ItemId,
                BaselineScore = scored.BaselineScore,
                AiScoreDelta = 0,
                FinalScore = finalScore,
                Recommendation = recommendation,
                HardBlocks = hardBlocks,
                EngineRationale = rationale,
                BlockedReason = hardBlocks.Count > 0
                    ? string.Join("; ", hardBlocks.Select(b => $"{b.Code}: {b.Message}"))
                    : null,
            });
        }

        var plan = new DecisionPlan
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            Items = planItems.AsReadOnly(),
        };

        await _planRepo.CreateAsync(plan);
        await _sessionService.TransitionAsync(sessionId, SessionStatus.PLANNED);

        return plan;
    }

    /// <summary>
    /// Gets the current plan for a session.
    /// </summary>
    public async Task<DecisionPlan?> GetCurrentPlanAsync(Guid sessionId) =>
        await _planRepo.GetCurrentBySessionIdAsync(sessionId);

    /// <summary>
    /// Gets hard blocks for a specific item in the current plan.
    /// </summary>
    public async Task<IReadOnlyList<HardBlock>> GetHardBlocksForItemAsync(Guid sessionId, string itemId)
    {
        var plan = await _planRepo.GetCurrentBySessionIdAsync(sessionId);
        if (plan is null) return [];

        var planItem = plan.Items.FirstOrDefault(i => i.ItemId == itemId);
        return planItem?.HardBlocks ?? [];
    }
}
