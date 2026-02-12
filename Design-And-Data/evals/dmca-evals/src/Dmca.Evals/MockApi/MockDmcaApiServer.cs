using System.Text.Json;

namespace Dmca.Evals.MockApi;

public sealed class MockDmcaApiServer
{
    private readonly MockDataStore _store;

    public List<string> CreatedProposalsJson { get; } = new();

    public MockDmcaApiServer(MockDataStore store) => _store = store;

    public string CallTool(string toolName, string argumentsJson)
    {
        switch (toolName)
        {
            case "get_session":
                return _store.GetSessionJson();

            case "get_inventory_latest":
                return _store.GetInventoryLatestJson();

            case "get_plan_current":
                return _store.GetPlanCurrentJson();

            case "get_inventory_item":
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var itemId = doc.RootElement.GetProperty("itemId").GetString() ?? "";
                return _store.GetInventoryItemJson(itemId);
            }

            case "get_hardblocks":
            {
                using var doc = JsonDocument.Parse(argumentsJson);
                var itemId = doc.RootElement.GetProperty("itemId").GetString() ?? "";
                return _store.GetHardBlocksJson(itemId);
            }

            case "create_proposal":
                CreatedProposalsJson.Add(argumentsJson);
                return JsonSerializer.Serialize(new { proposalId = Guid.NewGuid(), status = "PENDING" });

            case "list_proposals":
                return JsonSerializer.Serialize(new { proposals = Array.Empty<object>() });

            case "get_proposal":
                return JsonSerializer.Serialize(new { proposal = (object?)null, diff = (object?)null });

            default:
                throw new InvalidOperationException($"Unknown tool: {toolName}");
        }
    }
}
