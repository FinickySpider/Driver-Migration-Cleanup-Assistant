using Dmca.Core.Models;

namespace Dmca.App.ViewModels;

/// <summary>
/// Helper for inventory type filter ComboBox items.
/// </summary>
public sealed class InventoryTypeOption
{
    public required string Label { get; init; }
    public required InventoryItemType? Type { get; init; }

    public override string ToString() => Label;
}
