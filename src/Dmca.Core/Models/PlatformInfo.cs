namespace Dmca.Core.Models;

/// <summary>
/// Platform information collected from WMI during scan.
/// </summary>
public sealed class PlatformInfo
{
    public string? MotherboardVendor { get; init; }
    public string? MotherboardProduct { get; init; }
    public string? Cpu { get; init; }
    public string? OsVersion { get; init; }
}
