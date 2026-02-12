using Microsoft.Win32;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.App.Collectors;

/// <summary>
/// Collects installed programs from the Windows registry uninstall keys.
/// Reads both 64-bit and 32-bit (WOW6432Node) locations.
/// </summary>
public sealed class InstalledProgramsCollector : IInventoryCollector
{
    public InventoryItemType ItemType => InventoryItemType.APP;

    private static readonly string[] UninstallKeyPaths =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
    ];

    public Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default)
    {
        var items = new List<InventoryItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var keyPath in UninstallKeyPaths)
        {
            ct.ThrowIfCancellationRequested();
            CollectFromRegistryKey(Registry.LocalMachine, keyPath, items, seen, ct);
        }

        // Also check HKCU for per-user installs
        CollectFromRegistryKey(Registry.CurrentUser, UninstallKeyPaths[0], items, seen, ct);

        return Task.FromResult<IReadOnlyList<InventoryItem>>(items.AsReadOnly());
    }

    private static void CollectFromRegistryKey(
        RegistryKey root,
        string keyPath,
        List<InventoryItem> items,
        HashSet<string> seen,
        CancellationToken ct)
    {
        using var uninstallKey = root.OpenSubKey(keyPath);
        if (uninstallKey is null) return;

        foreach (var subKeyName in uninstallKey.GetSubKeyNames())
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var subKey = uninstallKey.OpenSubKey(subKeyName);
                if (subKey is null) continue;

                var displayName = subKey.GetValue("DisplayName")?.ToString();
                if (string.IsNullOrWhiteSpace(displayName)) continue;

                // Skip system components and updates
                var systemComponent = subKey.GetValue("SystemComponent");
                if (systemComponent is int sc && sc == 1) continue;

                var parentName = subKey.GetValue("ParentDisplayName")?.ToString();
                if (!string.IsNullOrWhiteSpace(parentName)) continue; // skip patches/updates

                var itemId = $"app:{subKeyName}";
                if (!seen.Add(itemId)) continue;

                var publisher = subKey.GetValue("Publisher")?.ToString();
                var version = subKey.GetValue("DisplayVersion")?.ToString();
                var installLocation = subKey.GetValue("InstallLocation")?.ToString();
                var installDateStr = subKey.GetValue("InstallDate")?.ToString();

                DateTime? installDate = null;
                if (installDateStr is not null && DateTime.TryParseExact(
                    installDateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var parsed))
                {
                    installDate = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }

                items.Add(new InventoryItem
                {
                    ItemId = itemId,
                    ItemType = InventoryItemType.APP,
                    DisplayName = displayName,
                    Vendor = publisher,
                    Version = version,
                    InstallDate = installDate,
                    Paths = !string.IsNullOrWhiteSpace(installLocation)
                        ? [installLocation]
                        : null,
                });
            }
            catch
            {
                // Skip entries that throw access denied or other errors
            }
        }
    }
}
