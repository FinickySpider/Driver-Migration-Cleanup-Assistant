using System.Text.Json.Serialization;

namespace Dmca.Core.Models;

/// <summary>
/// A single inventory item — driver, service, driver-store package, or installed program.
/// Matches inventory.json → InventoryItem schema.
/// Item IDs follow the pattern: drv:|svc:|pkg:|app: + identifier.
/// </summary>
public sealed class InventoryItem
{
    public required string ItemId { get; init; }
    public required InventoryItemType ItemType { get; init; }
    public required string DisplayName { get; init; }
    public string? Vendor { get; init; }
    public string? Provider { get; init; }
    public string? Version { get; init; }
    public string? DriverInf { get; init; }
    public string? DriverStorePublishedName { get; init; }
    public List<string>? DeviceHardwareIds { get; init; }
    public bool? Present { get; init; }
    public bool? Running { get; init; }
    public int? StartType { get; init; }
    public SignatureInfo? Signature { get; init; }
    public List<string>? Paths { get; init; }
    public DateTime? InstallDate { get; init; }
    public DateTime? LastLoadedDate { get; init; }
    public List<string>? Dependencies { get; init; }
}
