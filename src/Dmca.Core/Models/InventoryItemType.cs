namespace Dmca.Core.Models;

/// <summary>
/// Inventory item type prefix mapping:
/// DRIVER → drv:, SERVICE → svc:, DRIVER_PACKAGE → pkg:, APP → app:
/// </summary>
public enum InventoryItemType
{
    DRIVER,
    SERVICE,
    DRIVER_PACKAGE,
    APP
}
