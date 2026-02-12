using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Core.Services;

/// <summary>
/// CRUD and lifecycle management for proposals.
/// </summary>
public sealed class ProposalService
{
    private const int MaxChangesPerProposal = 5;

    private readonly IProposalRepository _proposalRepo;

    public ProposalService(IProposalRepository proposalRepo)
    {
        _proposalRepo = proposalRepo;
    }

    /// <summary>
    /// Creates a new PENDING proposal.
    /// </summary>
    public async Task<Proposal> CreateAsync(
        Guid sessionId,
        string title,
        IReadOnlyList<ProposalChange> changes,
        IReadOnlyList<Evidence>? evidence = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (changes.Count == 0)
            throw new ArgumentException("Proposal must have at least one change.");

        if (changes.Count > MaxChangesPerProposal)
            throw new ArgumentException($"Proposal cannot exceed {MaxChangesPerProposal} changes (got {changes.Count}).");

        var risk = ComputeRisk(changes);
        var now = DateTime.UtcNow;

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Title = title,
            Status = ProposalStatus.PENDING,
            Risk = risk,
            CreatedAt = now,
            UpdatedAt = now,
            Changes = changes,
            Evidence = evidence ?? [],
        };

        await _proposalRepo.CreateAsync(proposal);
        return proposal;
    }

    /// <summary>
    /// Gets a proposal by ID.
    /// </summary>
    public async Task<Proposal?> GetByIdAsync(Guid id) =>
        await _proposalRepo.GetByIdAsync(id);

    /// <summary>
    /// Lists all proposals for a session.
    /// </summary>
    public async Task<IReadOnlyList<Proposal>> ListBySessionAsync(Guid sessionId) =>
        await _proposalRepo.GetBySessionIdAsync(sessionId);

    /// <summary>
    /// Approves a proposal (UI-only action).
    /// </summary>
    public async Task ApproveAsync(Guid proposalId)
    {
        var proposal = await _proposalRepo.GetByIdAsync(proposalId)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        if (proposal.Status != ProposalStatus.PENDING)
            throw new InvalidOperationException(
                $"Cannot approve proposal in status {proposal.Status}. Must be PENDING.");

        await _proposalRepo.UpdateStatusAsync(proposalId, ProposalStatus.APPROVED, DateTime.UtcNow);
    }

    /// <summary>
    /// Rejects a proposal (UI-only action).
    /// </summary>
    public async Task RejectAsync(Guid proposalId)
    {
        var proposal = await _proposalRepo.GetByIdAsync(proposalId)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        if (proposal.Status != ProposalStatus.PENDING)
            throw new InvalidOperationException(
                $"Cannot reject proposal in status {proposal.Status}. Must be PENDING.");

        await _proposalRepo.UpdateStatusAsync(proposalId, ProposalStatus.REJECTED, DateTime.UtcNow);
    }

    /// <summary>
    /// Computes the estimated risk for a set of changes.
    /// </summary>
    internal static EstimatedRisk ComputeRisk(IReadOnlyList<ProposalChange> changes)
    {
        // HIGH if any score_delta > 15 or any recommendation change to REMOVE_STAGE_1
        if (changes.Any(c =>
            (c.Type == "score_delta" && Math.Abs(c.Delta ?? 0) > 15) ||
            (c.Type == "recommendation" && c.Value == "REMOVE_STAGE_1")))
            return EstimatedRisk.HIGH;

        // MEDIUM if more than 2 changes or any action_add
        if (changes.Count > 2 || changes.Any(c => c.Type == "action_add"))
            return EstimatedRisk.MEDIUM;

        return EstimatedRisk.LOW;
    }
}
