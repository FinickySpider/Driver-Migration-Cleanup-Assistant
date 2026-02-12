namespace Dmca.Core.Models;

/// <summary>
/// Summary statistics for an inventory snapshot.
/// </summary>
public sealed class SnapshotSummary
{
    public int Drivers { get; init; }
    public int Services { get; init; }
    public int Packages { get; init; }
    public int Apps { get; init; }
    public PlatformInfo? Platform { get; init; }
}
