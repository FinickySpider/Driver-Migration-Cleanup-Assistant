using System.Management;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.App.Collectors;

/// <summary>
/// Enriches driver items with device-presence information.
/// Uses Win32_PnPEntity.ConfigManagerErrorCode to determine present vs ghost devices.
/// This collector augments data from PnpDriverCollector; it returns items of type DRIVER
/// with the <see cref="InventoryItem.Present"/> field populated.
/// </summary>
public sealed class DevicePresenceCollector : IInventoryCollector
{
    public InventoryItemType ItemType => InventoryItemType.DRIVER;

    /// <summary>
    /// Returns DRIVER items with Present set based on PnPEntity availability.
    /// Items with ConfigManagerErrorCode == 0 and Status == "OK" are considered present.
    /// </summary>
    public Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default)
    {
        var items = new List<InventoryItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, DeviceID, Manufacturer, ConfigManagerErrorCode, Status " +
            "FROM Win32_PnPEntity");

        foreach (var obj in searcher.Get())
        {
            ct.ThrowIfCancellationRequested();

            var deviceId = obj["DeviceID"]?.ToString();
            var name = obj["Name"]?.ToString();
            if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(name))
                continue;

            // Build a stable key from device ID
            var itemId = $"drv:{SanitizeDeviceId(deviceId)}";
            if (!seen.Add(itemId))
                continue;

            var errorCode = obj["ConfigManagerErrorCode"];
            var status = obj["Status"]?.ToString();

            // A device is "present" if error code is 0 and status is OK
            var isPresent = errorCode is uint code
                ? code == 0 && string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase)
                : string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase);

            items.Add(new InventoryItem
            {
                ItemId = itemId,
                ItemType = InventoryItemType.DRIVER,
                DisplayName = name,
                Vendor = obj["Manufacturer"]?.ToString(),
                Present = isPresent,
                DeviceHardwareIds = new List<string> { deviceId },
            });
        }

        return Task.FromResult<IReadOnlyList<InventoryItem>>(items.AsReadOnly());
    }

    private static string SanitizeDeviceId(string deviceId)
    {
        // Replace characters invalid for item IDs
        return deviceId
            .Replace('\\', '.')
            .Replace('&', '.')
            .Replace(' ', '_');
    }
}
