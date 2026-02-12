using Dmca.Core.Models;

namespace Dmca.Core.Interfaces;

/// <summary>
/// Repository for audit log persistence.
/// </summary>
public interface IAuditLogRepository
{
    Task CreateAsync(AuditLogEntry entry);
    Task<IReadOnlyList<AuditLogEntry>> GetBySessionIdAsync(Guid sessionId);
    Task<IReadOnlyList<AuditLogEntry>> GetByActionIdAsync(Guid actionId);
}
