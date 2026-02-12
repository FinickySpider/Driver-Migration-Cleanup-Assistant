using System.Management;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.App.Collectors;

/// <summary>
/// Collects platform information from WMI: motherboard, CPU, OS.
/// </summary>
public sealed class WmiPlatformInfoCollector : IPlatformInfoCollector
{
    public Task<PlatformInfo> CollectAsync(CancellationToken ct = default)
    {
        var mbVendor = (string?)null;
        var mbProduct = (string?)null;
        var cpu = (string?)null;
        var osVersion = (string?)null;

        // Motherboard
        try
        {
            using var boardSearcher = new ManagementObjectSearcher(
                "SELECT Manufacturer, Product FROM Win32_BaseBoard");
            foreach (var obj in boardSearcher.Get())
            {
                ct.ThrowIfCancellationRequested();
                mbVendor = obj["Manufacturer"]?.ToString();
                mbProduct = obj["Product"]?.ToString();
                break; // only need first
            }
        }
        catch
        {
            // WMI may fail on some systems; leave nulls
        }

        // CPU
        try
        {
            using var cpuSearcher = new ManagementObjectSearcher(
                "SELECT Name FROM Win32_Processor");
            foreach (var obj in cpuSearcher.Get())
            {
                ct.ThrowIfCancellationRequested();
                cpu = obj["Name"]?.ToString()?.Trim();
                break;
            }
        }
        catch
        {
            // Leave null
        }

        // OS
        try
        {
            using var osSearcher = new ManagementObjectSearcher(
                "SELECT Version FROM Win32_OperatingSystem");
            foreach (var obj in osSearcher.Get())
            {
                ct.ThrowIfCancellationRequested();
                osVersion = obj["Version"]?.ToString();
                break;
            }
        }
        catch
        {
            // Leave null
        }

        var platformInfo = new PlatformInfo
        {
            MotherboardVendor = mbVendor,
            MotherboardProduct = mbProduct,
            Cpu = cpu,
            OsVersion = osVersion,
        };

        return Task.FromResult(platformInfo);
    }
}
