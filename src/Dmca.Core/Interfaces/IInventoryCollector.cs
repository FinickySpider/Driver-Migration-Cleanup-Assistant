using Dmca.Core.Models;

namespace Dmca.Core.Interfaces;

/// <summary>
/// Interface for inventory collectors. Each collector gathers one type of inventory item.
/// </summary>
public interface IInventoryCollector
{
    InventoryItemType ItemType { get; }
    Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default);
}
