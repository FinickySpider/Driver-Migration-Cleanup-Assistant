using Dmca.Core.Models;

namespace Dmca.Core.Interfaces;

/// <summary>
/// Repository for Proposal persistence.
/// </summary>
public interface IProposalRepository
{
    Task CreateAsync(Proposal proposal);
    Task<Proposal?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Proposal>> GetBySessionIdAsync(Guid sessionId);
    Task UpdateStatusAsync(Guid id, ProposalStatus status, DateTime updatedAt);
}
