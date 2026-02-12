namespace Dmca.Core.Models;

/// <summary>
/// Status of a proposal. Matches proposal.json → ProposalStatus.
/// </summary>
public enum ProposalStatus
{
    PENDING,
    APPROVED,
    REJECTED,
}

/// <summary>
/// Estimated risk level for a proposal.
/// </summary>
public enum EstimatedRisk
{
    LOW,
    MEDIUM,
    HIGH,
}
