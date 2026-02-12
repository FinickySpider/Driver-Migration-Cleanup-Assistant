namespace Dmca.Core.Models;

/// <summary>
/// A pending set of proposed changes to the decision plan.
/// Matches proposal.json → Proposal schema.
/// Created by the AI Advisor; approved/rejected by the user.
/// </summary>
public sealed class Proposal
{
    public required Guid Id { get; init; }
    public required Guid SessionId { get; init; }
    public required string Title { get; init; }
    public ProposalStatus Status { get; set; }
    public EstimatedRisk Risk { get; set; }
    public required DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    public required IReadOnlyList<ProposalChange> Changes { get; init; }
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
}
