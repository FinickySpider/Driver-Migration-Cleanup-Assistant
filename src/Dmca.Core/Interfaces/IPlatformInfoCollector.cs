using Dmca.Core.Models;

namespace Dmca.Core.Interfaces;

/// <summary>
/// Collects platform information (motherboard, CPU, OS) from WMI.
/// </summary>
public interface IPlatformInfoCollector
{
    Task<PlatformInfo> CollectAsync(CancellationToken ct = default);
}
