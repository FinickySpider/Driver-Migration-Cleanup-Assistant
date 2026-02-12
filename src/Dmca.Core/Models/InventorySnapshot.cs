namespace Dmca.Core.Models;

/// <summary>
/// An immutable point-in-time scan of all drivers, services, packages, and apps.
/// Once persisted, a snapshot is never modified.
/// Matches inventory.json → InventorySnapshot schema.
/// </summary>
public sealed class InventorySnapshot
{
    public required Guid Id { get; init; }
    public required Guid SessionId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required SnapshotSummary Summary { get; init; }
    public required IReadOnlyList<InventoryItem> Items { get; init; }
}
