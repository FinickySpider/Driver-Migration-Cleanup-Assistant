using Dmca.Core.Models;

namespace Dmca.Core.Interfaces;

/// <summary>
/// Repository for InventorySnapshot persistence.
/// Create and Get only — snapshots are immutable once persisted.
/// </summary>
public interface ISnapshotRepository
{
    Task CreateAsync(InventorySnapshot snapshot);
    Task<InventorySnapshot?> GetByIdAsync(Guid id);
    Task<InventorySnapshot?> GetLatestBySessionIdAsync(Guid sessionId);
    Task<IReadOnlyList<InventoryItem>> GetItemsBySnapshotIdAsync(Guid snapshotId);
}
