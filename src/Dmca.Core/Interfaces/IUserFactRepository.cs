using Dmca.Core.Models;

namespace Dmca.Core.Interfaces;

/// <summary>
/// Repository for UserFact append-only persistence.
/// No update or delete operations — facts are immutable once written.
/// </summary>
public interface IUserFactRepository
{
    Task AddAsync(UserFact fact);
    Task AddRangeAsync(IEnumerable<UserFact> facts);
    Task<IReadOnlyList<UserFact>> GetBySessionIdAsync(Guid sessionId);
}
