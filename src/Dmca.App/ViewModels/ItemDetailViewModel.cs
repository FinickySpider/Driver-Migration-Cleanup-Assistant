using CommunityToolkit.Mvvm.ComponentModel;
using Dmca.Core.Models;

namespace Dmca.App.ViewModels;

/// <summary>
/// Detail pane for a selected inventory item.
/// </summary>
public sealed partial class ItemDetailViewModel : PageViewModel
{
    public override string Title => "Item Detail";

    [ObservableProperty]
    private InventoryItem? _item;

    [ObservableProperty]
    private PlanItem? _planItem;

    public void ShowItem(InventoryItem item, PlanItem? planItem = null)
    {
        Item = item;
        PlanItem = planItem;
    }
}
