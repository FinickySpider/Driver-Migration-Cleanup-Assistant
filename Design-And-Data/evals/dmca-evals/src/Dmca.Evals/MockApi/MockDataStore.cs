using System.Text.Json;
using Dmca.Evals.Models;

namespace Dmca.Evals.MockApi;

public sealed class MockDataStore
{
    public Fixture Fixture { get; }
    public bool SimulateToolFailure { get; }

    public MockDataStore(Fixture fixture)
    {
        Fixture = fixture;
        SimulateToolFailure = fixture.Expect.SimulateToolFailure;
    }

    public string GetPlanCurrentJson()
    {
        if (SimulateToolFailure) throw new InvalidOperationException("Simulated tool failure: get_plan_current");
        return JsonSerializer.Serialize(new { items = Fixture.Plan.Items });
    }

    public string GetInventoryItemJson(string itemId)
    {
        if (SimulateToolFailure) throw new InvalidOperationException("Simulated tool failure: get_inventory_item");
        var item = Fixture.Inventory.Items.FirstOrDefault(x => x.ItemId == itemId);
        if (item is null) throw new KeyNotFoundException($"Item not found: {itemId}");
        return JsonSerializer.Serialize(item);
    }

    public string GetHardBlocksJson(string itemId)
    {
        if (SimulateToolFailure) throw new InvalidOperationException("Simulated tool failure: get_hardblocks");
        var planItem = Fixture.Plan.Items.FirstOrDefault(x => x.ItemId == itemId);
        return JsonSerializer.Serialize(new { itemId, hardBlocks = planItem?.HardBlocks ?? new List<PlanItem.HardBlock>() });
    }

    public string GetInventoryLatestJson()
    {
        if (SimulateToolFailure) throw new InvalidOperationException("Simulated tool failure: get_inventory_latest");
        return JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            createdAt = DateTimeOffset.UtcNow,
            summary = new
            {
                counts = new
                {
                    drivers = Fixture.Inventory.Items.Count(i => i.ItemType == "DRIVER"),
                    services = Fixture.Inventory.Items.Count(i => i.ItemType == "SERVICE"),
                    packages = Fixture.Inventory.Items.Count(i => i.ItemType == "DRIVER_PACKAGE"),
                    apps = Fixture.Inventory.Items.Count(i => i.ItemType == "APP")
                }
            }
        });
    }

    public string GetSessionJson()
    {
        if (SimulateToolFailure) throw new InvalidOperationException("Simulated tool failure: get_session");
        return JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            createdAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            updatedAt = DateTimeOffset.UtcNow,
            status = "PLANNED",
            appVersion = "evals-1.0.0"
        });
    }
}
