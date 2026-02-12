using System.Text.Json;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Services;

namespace Dmca.Core.AI;

/// <summary>
/// Dispatches AI tool calls to the appropriate service methods.
/// Only handles the 8 allowed tools — anything else is rejected.
/// </summary>
public sealed class ToolDispatcher
{
    private readonly SessionService _sessionService;
    private readonly ISnapshotRepository _snapshotRepo;
    private readonly PlanService _planService;
    private readonly ProposalService _proposalService;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public ToolDispatcher(
        SessionService sessionService,
        ISnapshotRepository snapshotRepo,
        PlanService planService,
        ProposalService proposalService)
    {
        _sessionService = sessionService;
        _snapshotRepo = snapshotRepo;
        _planService = planService;
        _proposalService = proposalService;
    }

    /// <summary>
    /// Dispatches a tool call and returns the JSON result.
    /// Throws if the tool is not allowed.
    /// </summary>
    public async Task<string> DispatchAsync(string toolName, string argumentsJson, Guid sessionId)
    {
        if (!AiSafetyGuard.IsAllowedTool(toolName))
            return JsonSerializer.Serialize(new { error = $"Tool '{toolName}' is not allowed." });

        try
        {
            return toolName switch
            {
                "get_session" => await HandleGetSession(sessionId),
                "get_inventory_latest" => await HandleGetInventoryLatest(sessionId),
                "get_inventory_item" => await HandleGetInventoryItem(argumentsJson, sessionId),
                "get_plan_current" => await HandleGetPlanCurrent(sessionId),
                "get_hardblocks" => await HandleGetHardBlocks(argumentsJson, sessionId),
                "create_proposal" => await HandleCreateProposal(argumentsJson, sessionId),
                "list_proposals" => await HandleListProposals(sessionId),
                "get_proposal" => await HandleGetProposal(argumentsJson),
                _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" }),
            };
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> HandleGetSession(Guid sessionId)
    {
        var session = await _sessionService.GetSessionAsync(sessionId);
        return session is null
            ? JsonSerializer.Serialize(new { error = "Session not found." })
            : JsonSerializer.Serialize(session, JsonOpts);
    }

    private async Task<string> HandleGetInventoryLatest(Guid sessionId)
    {
        var snapshot = await _snapshotRepo.GetLatestBySessionIdAsync(sessionId);
        if (snapshot is null)
            return JsonSerializer.Serialize(new { error = "No snapshot found." });

        // Return summary only (not full items)
        return JsonSerializer.Serialize(new
        {
            snapshotId = snapshot.Id,
            sessionId = snapshot.SessionId,
            createdAt = snapshot.CreatedAt,
            summary = snapshot.Summary,
        }, JsonOpts);
    }

    private async Task<string> HandleGetInventoryItem(string args, Guid sessionId)
    {
        var parsed = JsonSerializer.Deserialize<ItemIdArg>(args, JsonOpts);
        if (string.IsNullOrWhiteSpace(parsed?.ItemId))
            return JsonSerializer.Serialize(new { error = "Missing itemId parameter." });

        var snapshot = await _snapshotRepo.GetLatestBySessionIdAsync(sessionId);
        if (snapshot is null)
            return JsonSerializer.Serialize(new { error = "No snapshot found." });

        var item = snapshot.Items.FirstOrDefault(i => i.ItemId == parsed.ItemId);
        return item is null
            ? JsonSerializer.Serialize(new { error = $"Item {parsed.ItemId} not found." })
            : JsonSerializer.Serialize(item, JsonOpts);
    }

    private async Task<string> HandleGetPlanCurrent(Guid sessionId)
    {
        var plan = await _planService.GetCurrentPlanAsync(sessionId);
        return plan is null
            ? JsonSerializer.Serialize(new { error = "No current plan." })
            : JsonSerializer.Serialize(plan, JsonOpts);
    }

    private async Task<string> HandleGetHardBlocks(string args, Guid sessionId)
    {
        var parsed = JsonSerializer.Deserialize<ItemIdArg>(args, JsonOpts);
        if (string.IsNullOrWhiteSpace(parsed?.ItemId))
            return JsonSerializer.Serialize(new { error = "Missing itemId parameter." });

        var blocks = await _planService.GetHardBlocksForItemAsync(sessionId, parsed.ItemId);
        return JsonSerializer.Serialize(new { itemId = parsed.ItemId, hardBlocks = blocks }, JsonOpts);
    }

    private async Task<string> HandleCreateProposal(string args, Guid sessionId)
    {
        var parsed = JsonSerializer.Deserialize<CreateProposalArg>(args, JsonOpts);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Title))
            return JsonSerializer.Serialize(new { error = "Missing title." });

        if (parsed.Changes is null || parsed.Changes.Count == 0)
            return JsonSerializer.Serialize(new { error = "Missing changes." });

        // Validate through safety guard
        var plan = await _planService.GetCurrentPlanAsync(sessionId);
        var planItems = plan?.Items.ToList();
        var violations = AiSafetyGuard.ValidateProposal(parsed.Changes, planItems);
        if (violations.Count > 0)
            return JsonSerializer.Serialize(new { error = "Proposal validation failed.", violations });

        var proposal = await _proposalService.CreateAsync(
            sessionId, parsed.Title, parsed.Changes, parsed.Evidence);

        return JsonSerializer.Serialize(new
        {
            proposalId = proposal.Id,
            status = proposal.Status.ToString(),
            message = "Proposal created. Awaiting user approval.",
        }, JsonOpts);
    }

    private async Task<string> HandleListProposals(Guid sessionId)
    {
        var proposals = await _proposalService.ListBySessionAsync(sessionId);
        return JsonSerializer.Serialize(proposals, JsonOpts);
    }

    private async Task<string> HandleGetProposal(string args)
    {
        var parsed = JsonSerializer.Deserialize<ProposalIdArg>(args, JsonOpts);
        if (parsed?.ProposalId is null || parsed.ProposalId == Guid.Empty)
            return JsonSerializer.Serialize(new { error = "Missing proposalId parameter." });

        var proposal = await _proposalService.GetByIdAsync(parsed.ProposalId);
        return proposal is null
            ? JsonSerializer.Serialize(new { error = "Proposal not found." })
            : JsonSerializer.Serialize(proposal, JsonOpts);
    }

    // ── Argument DTOs ──

    private sealed class ItemIdArg
    {
        public string? ItemId { get; set; }
    }

    private sealed class ProposalIdArg
    {
        public Guid ProposalId { get; set; }
    }

    private sealed class CreateProposalArg
    {
        public string? Title { get; set; }
        public List<ProposalChange>? Changes { get; set; }
        public List<Evidence>? Evidence { get; set; }
    }
}
