namespace Dmca.Core.Models;

/// <summary>
/// The complete decision plan for a session. Contains scored and evaluated plan items.
/// Matches plan.json → DecisionPlan schema.
/// </summary>
public sealed class DecisionPlan
{
    public required Guid Id { get; init; }
    public required Guid SessionId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyList<PlanItem> Items { get; init; }
}
