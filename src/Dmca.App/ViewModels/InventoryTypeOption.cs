using Dmca.Core.Models;

namespace Dmca.App.ViewModels;

/// <summary>
/// Helper for inventory type filter ComboBox items.
/// </summary>
public sealed class InventoryTypeOption
{
    public required string Label { get; init; }
    public required string Type { get; init; }

    public InventoryItemType? ToItemType() => Enum.TryParse<InventoryItemType>(Type, out var t) ? t : null;

    public override string ToString() => Label;
}
