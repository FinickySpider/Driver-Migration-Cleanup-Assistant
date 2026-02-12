using System.Management;
using System.ServiceProcess;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.App.Collectors;

/// <summary>
/// Collects Windows services via ServiceController + WMI Win32_Service.
/// Populates: itemId (svc:ServiceName), itemType=SERVICE, displayName, vendor,
/// running, startType, dependencies, paths.
/// </summary>
public sealed class ServiceCollector : IInventoryCollector
{
    public InventoryItemType ItemType => InventoryItemType.SERVICE;

    public Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default)
    {
        // First pass: get rich details from WMI
        var wmiData = GetWmiServiceData(ct);

        // Second pass: use ServiceController for accurate status
        var items = new List<InventoryItem>();

        foreach (var svc in ServiceController.GetServices())
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var itemId = $"svc:{svc.ServiceName}";
                var isRunning = svc.Status == ServiceControllerStatus.Running;

                wmiData.TryGetValue(svc.ServiceName, out var wmi);

                items.Add(new InventoryItem
                {
                    ItemId = itemId,
                    ItemType = InventoryItemType.SERVICE,
                    DisplayName = svc.DisplayName,
                    Vendor = wmi?.PathName is not null ? ExtractVendorHint(wmi.PathName) : null,
                    Running = isRunning,
                    StartType = wmi?.StartMode switch
                    {
                        "Boot" => 0,
                        "System" => 1,
                        "Auto" => 2,
                        "Manual" => 3,
                        "Disabled" => 4,
                        _ => null,
                    },
                    Paths = wmi?.PathName is not null ? [wmi.PathName] : null,
                    Dependencies = svc.ServicesDependedOn.Length > 0
                        ? svc.ServicesDependedOn.Select(d => d.ServiceName).ToList()
                        : null,
                });
            }
            catch
            {
                // Skip services that throw access denied, etc.
            }
            finally
            {
                svc.Dispose();
            }
        }

        return Task.FromResult<IReadOnlyList<InventoryItem>>(items.AsReadOnly());
    }

    private static Dictionary<string, WmiServiceInfo> GetWmiServiceData(CancellationToken ct)
    {
        var result = new Dictionary<string, WmiServiceInfo>(StringComparer.OrdinalIgnoreCase);

        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, PathName, StartMode, Description FROM Win32_Service");

        foreach (var obj in searcher.Get())
        {
            ct.ThrowIfCancellationRequested();

            var name = obj["Name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name)) continue;

            result[name] = new WmiServiceInfo
            {
                PathName = obj["PathName"]?.ToString(),
                StartMode = obj["StartMode"]?.ToString(),
                Description = obj["Description"]?.ToString(),
            };
        }

        return result;
    }

    /// <summary>
    /// Attempts to extract a vendor hint from the executable path.
    /// e.g., "C:\Program Files\Intel\..." → "Intel"
    /// </summary>
    private static string? ExtractVendorHint(string pathName)
    {
        // Remove surrounding quotes
        var path = pathName.Trim('"');

        // Try to find a recognizable vendor folder
        var parts = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (KnownVendorFolders.Contains(part.ToLowerInvariant()))
                return part;
        }

        return null;
    }

    private static readonly HashSet<string> KnownVendorFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "intel", "nvidia", "amd", "realtek", "asus", "gigabyte", "msi",
        "qualcomm", "broadcom", "corsair", "razer", "logitech",
        "samsung", "kingston", "western digital", "seagate",
    };

    private sealed class WmiServiceInfo
    {
        public string? PathName { get; init; }
        public string? StartMode { get; init; }
        public string? Description { get; init; }
    }
}
