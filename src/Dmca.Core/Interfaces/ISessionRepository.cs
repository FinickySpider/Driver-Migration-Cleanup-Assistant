using Dmca.Core.Models;

namespace Dmca.Core.Interfaces;

/// <summary>
/// Repository for Session CRUD operations.
/// </summary>
public interface ISessionRepository
{
    Task<Session> CreateAsync(Session session);
    Task<Session?> GetByIdAsync(Guid id);
    Task<Session?> GetCurrentAsync();
    Task UpdateAsync(Session session);
}
