namespace Dmca.Core.Models;

/// <summary>
/// Categorization of a delta item change between two snapshots.
/// </summary>
public enum DeltaStatus
{
    /// <summary>Item was present in the pre-snapshot but absent in the post-snapshot.</summary>
    REMOVED,

    /// <summary>Item is present in both snapshots but one or more properties changed.</summary>
    CHANGED,

    /// <summary>Item is present and identical in both snapshots.</summary>
    UNCHANGED,

    /// <summary>Item exists only in the post-snapshot (newly appeared).</summary>
    ADDED,
}

/// <summary>
/// A single property-level change within a delta item.
/// </summary>
public sealed class PropertyChange
{
    public required string Property { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

/// <summary>
/// One item's status in the delta report.
/// </summary>
public sealed class DeltaReportItem
{
    public required string ItemId { get; init; }
    public required InventoryItemType ItemType { get; init; }
    public required string DisplayName { get; init; }
    public required DeltaStatus Status { get; init; }
    public IReadOnlyList<PropertyChange> Changes { get; init; } = [];
}

/// <summary>
/// Summary statistics for the delta report.
/// </summary>
public sealed class DeltaReportSummary
{
    public int Removed { get; init; }
    public int Changed { get; init; }
    public int Unchanged { get; init; }
    public int Added { get; init; }
}

/// <summary>
/// Full delta report comparing a pre-execution snapshot to a post-execution snapshot.
/// </summary>
public sealed class DeltaReport
{
    public required Guid Id { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid PreSnapshotId { get; init; }
    public required Guid PostSnapshotId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DeltaReportSummary Summary { get; init; }
    public required IReadOnlyList<DeltaReportItem> Items { get; init; }
    public required IReadOnlyList<string> NextSteps { get; init; }
}
