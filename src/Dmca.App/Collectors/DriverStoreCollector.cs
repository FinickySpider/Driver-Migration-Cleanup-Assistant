using System.Diagnostics;
using System.Text.RegularExpressions;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.App.Collectors;

/// <summary>
/// Collects driver-store packages using pnputil /enum-drivers.
/// Each published .inf in the driver store becomes a DRIVER_PACKAGE item.
/// </summary>
public sealed partial class DriverStoreCollector : IInventoryCollector
{
    public InventoryItemType ItemType => InventoryItemType.DRIVER_PACKAGE;

    public async Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default)
    {
        var output = await RunPnpUtilAsync(ct);
        return ParsePnpUtilOutput(output);
    }

    private static async Task<string> RunPnpUtilAsync(CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pnputil.exe",
                Arguments = "/enum-drivers",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return output;
    }

    internal static IReadOnlyList<InventoryItem> ParsePnpUtilOutput(string output)
    {
        var items = new List<InventoryItem>();

        // pnputil output groups are separated by blank lines
        // Each group has: Published Name, Original Name, Provider, Class Name, etc.
        var blocks = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var publishedName = ExtractField(block, "Published Name", "Published name");
            var originalName = ExtractField(block, "Original Name", "Original name");
            var providerName = ExtractField(block, "Driver package provider", "Provider Name");
            var className = ExtractField(block, "Class Name", "Class name");
            var version = ExtractField(block, "Driver Version", "Driver version");
            var signerName = ExtractField(block, "Signer Name", "Signer name");

            if (string.IsNullOrWhiteSpace(publishedName))
                continue;

            var displayName = !string.IsNullOrWhiteSpace(originalName)
                ? $"{originalName} ({className ?? "Unknown"})"
                : $"{publishedName} ({className ?? "Unknown"})";

            var isSigned = !string.IsNullOrWhiteSpace(signerName);
            var isMicrosoft = signerName?.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) == true;

            items.Add(new InventoryItem
            {
                ItemId = $"pkg:{publishedName}",
                ItemType = InventoryItemType.DRIVER_PACKAGE,
                DisplayName = displayName,
                Vendor = providerName,
                Provider = providerName,
                Version = version,
                DriverStorePublishedName = publishedName,
                DriverInf = originalName,
                Signature = new SignatureInfo
                {
                    Signed = isSigned,
                    Signer = signerName,
                    IsMicrosoft = isMicrosoft,
                    IsWHQL = isMicrosoft && isSigned,
                },
            });
        }

        return items.AsReadOnly();
    }

    private static string? ExtractField(string block, params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            // Match lines like "Published Name  :  oem42.inf"
            var pattern = $@"^\s*{Regex.Escape(fieldName)}\s*:\s*(.+)$";
            var match = Regex.Match(block, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();
        }
        return null;
    }
}
