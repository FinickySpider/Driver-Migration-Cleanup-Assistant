namespace Dmca.Core.Models;

/// <summary>
/// A single item in the decision plan with scores, recommendation, and rationale.
/// Matches plan.json → PlanItem schema.
/// </summary>
public sealed class PlanItem
{
    public required string ItemId { get; init; }
    public required int BaselineScore { get; init; }
    public int AiScoreDelta { get; set; }
    public int FinalScore { get; set; }
    public required Recommendation Recommendation { get; set; }
    public required IReadOnlyList<HardBlock> HardBlocks { get; init; }
    public required List<string> EngineRationale { get; init; }
    public List<string> AiRationale { get; set; } = [];
    public List<string> Notes { get; set; } = [];
    public string? BlockedReason { get; set; }
}
