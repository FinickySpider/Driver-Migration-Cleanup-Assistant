namespace Dmca.Core.Models;

/// <summary>
/// A hard block that prevents removal of an inventory item.
/// Matches plan.json → hardBlocks[] schema.
/// </summary>
public sealed class HardBlock
{
    public required string Code { get; init; }
    public required string Message { get; init; }
}
