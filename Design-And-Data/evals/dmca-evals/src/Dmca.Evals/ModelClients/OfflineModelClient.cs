using System.Text.Json;
using Dmca.Evals.Models;
using Dmca.Evals.MockApi;

namespace Dmca.Evals.ModelClients;

public sealed class OfflineModelClient : IModelClient
{
    public Task<(string assistantText, List<ToolCall> toolCalls)> RunAsync(
        Fixture fixture,
        MockDmcaApiServer toolRouter,
        CancellationToken ct)
    {
        var calls = new List<ToolCall>();
        string assistantText;

        try { calls.Add(Call(toolRouter, "get_plan_current", "{}", out _)); }
        catch
        {
            assistantText = "Tools failed. Please retry a rescan or reopen the session. I will not make assumptions.";
            return Task.FromResult((assistantText, calls));
        }

        if (fixture.Expect.ForceOfflineBadModel)
        {
            var bad = new { title = "Bad proposal (no evidence)", changes = new[] { new { type="score_delta", targetId=fixture.Plan.Items.First().ItemId, delta=20, reason="because reasons" } } };
            var badJson = JsonSerializer.Serialize(bad);
            calls.Add(Call(toolRouter, "create_proposal", badJson, out _));
            assistantText = "Created a proposal.";
            return Task.FromResult((assistantText, calls));
        }

        var targets = fixture.Plan.Items
            .Where(p => p.HardBlocks.Count == 0)
            .Where(p =>
            {
                var inv = fixture.Inventory.Items.FirstOrDefault(i => i.ItemId == p.ItemId);
                return inv != null && inv.Present == false && (inv.Vendor ?? "").Contains("Intel", StringComparison.OrdinalIgnoreCase);
            })
            .Take(3)
            .ToList();

        if (targets.Count == 0)
        {
            assistantText = "No safe high-confidence leftovers detected from the provided fixture. If you want, confirm old/new platform facts or provide more targets.";
            return Task.FromResult((assistantText, calls));
        }

        foreach (var t in targets)
        {
            calls.Add(Call(toolRouter, "get_inventory_item", JsonSerializer.Serialize(new { itemId = t.ItemId }), out _));
            calls.Add(Call(toolRouter, "get_hardblocks", JsonSerializer.Serialize(new { itemId = t.ItemId }), out _));
        }

        var changes = targets.Select(t => new { type="score_delta", targetId=t.ItemId, delta=15, reason="Non-present Intel leftover likely from previous platform migration." }).ToList<object>();

        var proposal = new
        {
            title = "Adjust scoring for Intel leftovers",
            notes = "Small conservative bump; review before execution.",
            changes,
            riskSummary = new { estimatedRisk = "LOW", restorePointRequired = true },
            evidence = new object[]
            {
                new { kind="inventory_field", path="item.vendor", value="Intel" },
                new { kind="inventory_field", path="item.present", value=false },
                new { kind="user_fact", path="userFacts.old_platform_vendor", value=fixture.UserFacts.GetValueOrDefault("old_platform_vendor","") }
            }
        };

        var proposalJson = JsonSerializer.Serialize(proposal);
        calls.Add(Call(toolRouter, "create_proposal", proposalJson, out _));
        assistantText = "I proposed a small scoring adjustment for non-present Intel leftovers. Review and approve if it matches your migration context.";
        return Task.FromResult((assistantText, calls));
    }

    private static ToolCall Call(MockDmcaApiServer router, string name, string args, out string toolResult)
    {
        toolResult = router.CallTool(name, args);
        return new ToolCall { Name = name, ArgumentsJson = args };
    }
}
