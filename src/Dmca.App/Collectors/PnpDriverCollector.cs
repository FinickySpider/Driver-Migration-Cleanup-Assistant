using System.Management;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.App.Collectors;

/// <summary>
/// Collects PnP signed drivers via WMI Win32_PnPSignedDriver.
/// Populates: itemId (drv:INF), itemType=DRIVER, displayName, vendor, provider,
/// version, driverInf, deviceHardwareIds, signature info.
/// </summary>
public sealed class PnpDriverCollector : IInventoryCollector
{
    public InventoryItemType ItemType => InventoryItemType.DRIVER;

    public Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default)
    {
        var items = new List<InventoryItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var searcher = new ManagementObjectSearcher(
            "SELECT DeviceName, DriverProviderName, DriverVersion, InfName, " +
            "Manufacturer, Signer, IsSigned, HardWareID, DeviceID " +
            "FROM Win32_PnPSignedDriver");

        foreach (var obj in searcher.Get())
        {
            ct.ThrowIfCancellationRequested();

            var infName = obj["InfName"]?.ToString();
            var deviceName = obj["DeviceName"]?.ToString();

            if (string.IsNullOrWhiteSpace(infName) || string.IsNullOrWhiteSpace(deviceName))
                continue;

            // Use INF name as primary key; deduplicate
            var itemId = $"drv:{infName}";
            if (!seen.Add(itemId))
                continue;

            var signer = obj["Signer"]?.ToString();
            var isSigned = obj["IsSigned"] is bool b && b;
            var isMicrosoft = signer?.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) == true;

            var hardwareId = obj["HardWareID"]?.ToString();
            var hwIds = string.IsNullOrWhiteSpace(hardwareId)
                ? null
                : new List<string> { hardwareId };

            items.Add(new InventoryItem
            {
                ItemId = itemId,
                ItemType = InventoryItemType.DRIVER,
                DisplayName = deviceName,
                Vendor = obj["Manufacturer"]?.ToString(),
                Provider = obj["DriverProviderName"]?.ToString(),
                Version = obj["DriverVersion"]?.ToString(),
                DriverInf = infName,
                DeviceHardwareIds = hwIds,
                Signature = new SignatureInfo
                {
                    Signed = isSigned,
                    Signer = signer,
                    IsMicrosoft = isMicrosoft,
                    IsWHQL = isMicrosoft && isSigned, // approximate: WHQL ≈ Microsoft + signed
                },
            });
        }

        return Task.FromResult<IReadOnlyList<InventoryItem>>(items.AsReadOnly());
    }
}
