using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Scoring;

namespace Dmca.Core.Services;

/// <summary>
/// Merges approved proposal changes into the decision plan.
/// Enforces delta clamping and hard-block protection.
/// </summary>
public sealed class PlanMergeService
{
    private readonly RulesConfig _rules;
    private readonly IPlanRepository _planRepo;
    private readonly IProposalRepository _proposalRepo;
    private readonly BaselineScorer _scorer;

    public PlanMergeService(
        RulesConfig rules,
        IPlanRepository planRepo,
        IProposalRepository proposalRepo)
    {
        _rules = rules;
        _planRepo = planRepo;
        _proposalRepo = proposalRepo;
        _scorer = new BaselineScorer(rules);
    }

    /// <summary>
    /// Merges an approved proposal into the current plan.
    /// Returns merge results with any skipped changes.
    /// </summary>
    public async Task<MergeResult> MergeProposalAsync(Guid sessionId, Guid proposalId, bool hasUserFact = false)
    {
        var proposal = await _proposalRepo.GetByIdAsync(proposalId)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        if (proposal.Status != ProposalStatus.APPROVED)
            throw new InvalidOperationException(
                $"Cannot merge proposal in status {proposal.Status}. Must be APPROVED.");

        var plan = await _planRepo.GetCurrentBySessionIdAsync(sessionId)
            ?? throw new InvalidOperationException($"No current plan for session {sessionId}.");

        var applied = new List<MergeChangeResult>();
        var skipped = new List<MergeChangeResult>();

        foreach (var change in proposal.Changes)
        {
            var planItem = plan.Items.FirstOrDefault(i => i.ItemId == change.TargetId);
            if (planItem is null)
            {
                skipped.Add(new MergeChangeResult(change, "Target item not found in plan."));
                continue;
            }

            // Hard-blocked items are non-removable regardless of proposals
            if (planItem.HardBlocks.Count > 0 && change.Type is "score_delta" or "recommendation")
            {
                skipped.Add(new MergeChangeResult(change, "Item is hard-blocked; change rejected."));
                continue;
            }

            switch (change.Type)
            {
                case "score_delta":
                    ApplyScoreDelta(planItem, change, hasUserFact, applied, skipped);
                    break;

                case "recommendation":
                    ApplyRecommendation(planItem, change, applied, skipped);
                    break;

                case "pin_protect":
                    planItem.Recommendation = Recommendation.KEEP;
                    planItem.AiRationale.Add($"Pinned: {change.Reason}");
                    applied.Add(new MergeChangeResult(change, "Applied."));
                    break;

                case "note_add":
                    planItem.Notes.Add(change.Note ?? change.Reason);
                    planItem.AiRationale.Add($"Note added: {change.Reason}");
                    applied.Add(new MergeChangeResult(change, "Applied."));
                    break;

                case "fact_request":
                    planItem.AiRationale.Add($"Fact requested: {change.Reason}");
                    applied.Add(new MergeChangeResult(change, "Fact request recorded."));
                    break;

                default:
                    skipped.Add(new MergeChangeResult(change, $"Unknown change type: {change.Type}"));
                    break;
            }
        }

        // Persist updated items
        foreach (var result in applied)
        {
            var item = plan.Items.FirstOrDefault(i => i.ItemId == result.Change.TargetId);
            if (item is not null)
                await _planRepo.UpdatePlanItemAsync(plan.Id, item);
        }

        return new MergeResult(proposalId, applied.AsReadOnly(), skipped.AsReadOnly());
    }

    private void ApplyScoreDelta(
        PlanItem item,
        ProposalChange change,
        bool hasUserFact,
        List<MergeChangeResult> applied,
        List<MergeChangeResult> skipped)
    {
        var delta = change.Delta ?? 0;
        var maxDelta = hasUserFact
            ? _rules.Limits.AiDeltaMaxWithUserFact
            : _rules.Limits.AiDeltaMax;
        var minDelta = hasUserFact
            ? -_rules.Limits.AiDeltaMaxWithUserFact
            : _rules.Limits.AiDeltaMin;

        // Clamp delta
        var clampedDelta = Math.Clamp(delta, minDelta, maxDelta);

        item.AiScoreDelta += clampedDelta;
        item.FinalScore = Math.Clamp(
            item.BaselineScore + item.AiScoreDelta,
            _rules.Limits.ScoreMin,
            _rules.Limits.ScoreMax);

        // Recompute recommendation from new final score
        var newRec = _scorer.GetRecommendation(item.FinalScore);
        item.Recommendation = Enum.Parse<Recommendation>(newRec);

        var clampNote = delta != clampedDelta
            ? $" (clamped from {delta} to {clampedDelta})"
            : "";
        item.AiRationale.Add($"Score delta {clampedDelta:+#;-#;0}{clampNote}: {change.Reason}");

        applied.Add(new MergeChangeResult(change,
            $"Applied delta {clampedDelta}{clampNote}. New score: {item.FinalScore}."));
    }

    private void ApplyRecommendation(
        PlanItem item,
        ProposalChange change,
        List<MergeChangeResult> applied,
        List<MergeChangeResult> skipped)
    {
        if (change.Value is null ||
            !Enum.TryParse<Recommendation>(change.Value, out var newRec))
        {
            skipped.Add(new MergeChangeResult(change, $"Invalid recommendation value: {change.Value}"));
            return;
        }

        item.Recommendation = newRec;
        item.AiRationale.Add($"Recommendation changed to {newRec}: {change.Reason}");
        applied.Add(new MergeChangeResult(change, $"Recommendation set to {newRec}."));
    }
}

/// <summary>
/// Result of merging a single proposal.
/// </summary>
public sealed record MergeResult(
    Guid ProposalId,
    IReadOnlyList<MergeChangeResult> Applied,
    IReadOnlyList<MergeChangeResult> Skipped);

/// <summary>
/// Result of applying or skipping a single change.
/// </summary>
public sealed record MergeChangeResult(
    ProposalChange Change,
    string Message);
