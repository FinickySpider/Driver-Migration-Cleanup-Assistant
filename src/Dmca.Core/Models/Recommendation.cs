namespace Dmca.Core.Models;

/// <summary>
/// Recommendation for an inventory item. Matches plan.json → Recommendation enum.
/// </summary>
public enum Recommendation
{
    KEEP,
    REVIEW,
    REMOVE_STAGE_1,
    REMOVE_STAGE_2,
    BLOCKED,
}
