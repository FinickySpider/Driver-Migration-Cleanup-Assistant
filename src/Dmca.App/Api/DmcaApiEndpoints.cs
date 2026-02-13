using System.Text.Json;
using Dmca.Core.AI;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dmca.App.Api;

/// <summary>
/// Maps all DMCA REST API endpoints using ASP.NET Core minimal APIs.
/// Loopback-only on 127.0.0.1:17831.
/// </summary>
public static class DmcaApiEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    /// <summary>
    /// Maps all v1 API routes onto the given endpoint route builder.
    /// </summary>
    public static void MapDmcaApi(
        this IEndpointRouteBuilder app,
        SessionService sessionService,
        ISnapshotRepository snapshotRepo,
        PlanService planService,
        ProposalService proposalService,
        PlanMergeService mergeService,
        RescanService? rescanService = null)
    {
        // ── Session ──
        app.MapGet("/v1/session", async () =>
        {
            var session = await sessionService.GetCurrentSessionAsync();
            return session is null
                ? Results.NotFound(new { error = "No active session." })
                : Results.Json(session, JsonOpts);
        });

        // ── Inventory ──
        app.MapGet("/v1/inventory/latest", async () =>
        {
            var session = await sessionService.GetCurrentSessionAsync();
            if (session is null)
                return Results.NotFound(new { error = "No active session." });

            var snapshot = await snapshotRepo.GetLatestBySessionIdAsync(session.Id);
            if (snapshot is null)
                return Results.NotFound(new { error = "No snapshot found." });

            return Results.Json(new
            {
                snapshotId = snapshot.Id,
                sessionId = snapshot.SessionId,
                createdAt = snapshot.CreatedAt,
                summary = snapshot.Summary,
            }, JsonOpts);
        });

        app.MapGet("/v1/inventory/item/{itemId}", async (string itemId) =>
        {
            var session = await sessionService.GetCurrentSessionAsync();
            if (session is null)
                return Results.NotFound(new { error = "No active session." });

            var snapshot = await snapshotRepo.GetLatestBySessionIdAsync(session.Id);
            if (snapshot is null)
                return Results.NotFound(new { error = "No snapshot found." });

            var item = snapshot.Items.FirstOrDefault(i => i.ItemId == itemId);
            return item is null
                ? Results.NotFound(new { error = $"Item {itemId} not found." })
                : Results.Json(item, JsonOpts);
        });

        // ── Plan ──
        app.MapGet("/v1/plan/current", async () =>
        {
            var session = await sessionService.GetCurrentSessionAsync();
            if (session is null)
                return Results.NotFound(new { error = "No active session." });

            var plan = await planService.GetCurrentPlanAsync(session.Id);
            return plan is null
                ? Results.NotFound(new { error = "No current plan." })
                : Results.Json(plan, JsonOpts);
        });

        // ── Hard Blocks ──
        app.MapGet("/v1/policy/hardblocks/{itemId}", async (string itemId) =>
        {
            var session = await sessionService.GetCurrentSessionAsync();
            if (session is null)
                return Results.NotFound(new { error = "No active session." });

            var blocks = await planService.GetHardBlocksForItemAsync(session.Id, itemId);
            return Results.Json(new { itemId, hardBlocks = blocks }, JsonOpts);
        });

        // ── Proposals ──
        app.MapGet("/v1/proposals", async () =>
        {
            var session = await sessionService.GetCurrentSessionAsync();
            if (session is null)
                return Results.NotFound(new { error = "No active session." });

            var proposals = await proposalService.ListBySessionAsync(session.Id);
            return Results.Json(proposals, JsonOpts);
        });

        app.MapPost("/v1/proposals", async (HttpRequest request) =>
        {
            var session = await sessionService.GetCurrentSessionAsync();
            if (session is null)
                return Results.NotFound(new { error = "No active session." });

            var body = await JsonSerializer.DeserializeAsync<CreateProposalRequest>(
                request.Body, JsonOpts);

            if (body is null || string.IsNullOrWhiteSpace(body.Title))
                return Results.BadRequest(new { error = "Missing title." });

            if (body.Changes is null || body.Changes.Count == 0)
                return Results.BadRequest(new { error = "Missing changes." });

            // Safety validation
            var plan = await planService.GetCurrentPlanAsync(session.Id);
            var violations = AiSafetyGuard.ValidateProposal(body.Changes, plan?.Items.ToList());
            if (violations.Count > 0)
                return Results.BadRequest(new { error = "Validation failed.", violations });

            try
            {
                var proposal = await proposalService.CreateAsync(
                    session.Id, body.Title, body.Changes, body.Evidence);

                return Results.Created($"/v1/proposals/{proposal.Id}", proposal);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/v1/proposals/{proposalId}", async (string proposalId) =>
        {
            if (!Guid.TryParse(proposalId, out var id))
                return Results.BadRequest(new { error = "Invalid proposal ID." });

            var proposal = await proposalService.GetByIdAsync(id);
            return proposal is null
                ? Results.NotFound(new { error = "Proposal not found." })
                : Results.Json(proposal, JsonOpts);
        });

        // ── UI-only actions ──
        app.MapPost("/v1/proposals/{proposalId}/approve", async (string proposalId) =>
        {
            if (!Guid.TryParse(proposalId, out var id))
                return Results.BadRequest(new { error = "Invalid proposal ID." });

            try
            {
                await proposalService.ApproveAsync(id);

                // Merge into plan
                var session = await sessionService.GetCurrentSessionAsync();
                if (session is not null)
                {
                    var result = await mergeService.MergeProposalAsync(session.Id, id);
                    return Results.Json(new
                    {
                        message = "Proposal approved and merged.",
                        applied = result.Applied.Count,
                        skipped = result.Skipped.Count,
                    }, JsonOpts);
                }

                return Results.Ok(new { message = "Approved." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/v1/proposals/{proposalId}/reject", async (string proposalId) =>
        {
            if (!Guid.TryParse(proposalId, out var id))
                return Results.BadRequest(new { error = "Invalid proposal ID." });

            try
            {
                await proposalService.RejectAsync(id);
                return Results.Ok(new { message = "Proposal rejected." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // ── Rescan ──
        app.MapPost("/v1/rescan", async () =>
        {
            if (rescanService is null)
                return Results.StatusCode(501);

            var session = await sessionService.GetCurrentSessionAsync();
            if (session is null)
                return Results.NotFound(new { error = "No active session." });

            try
            {
                var snapshot = await rescanService.RescanAsync(session.Id);
                return Results.Json(new
                {
                    message = "Rescan complete. Session completed.",
                    snapshotId = snapshot.Id,
                    summary = snapshot.Summary,
                }, JsonOpts);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Placeholder for execute — will be implemented in Phase 3
        app.MapPost("/v1/actions/queue/execute", () =>
            Results.StatusCode(501));
    }

    private sealed class CreateProposalRequest
    {
        public string? Title { get; set; }
        public List<ProposalChange>? Changes { get; set; }
        public List<Evidence>? Evidence { get; set; }
    }
}
