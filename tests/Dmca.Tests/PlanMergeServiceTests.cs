using Dmca.Core.Models;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="PlanMergeService"/> merging approved proposals into plans.
/// </summary>
public sealed class PlanMergeServiceTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
    private readonly PlanMergeService _mergeService;
    private readonly ProposalService _proposalService;
    private readonly PlanService _planService;
    private readonly SessionService _sessionService;
    private Guid _sessionId;

    public PlanMergeServiceTests()
    {
        _db = DmcaDbContext.InMemory($"merge_test_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();

        var sessionRepo = new SessionRepository(_db);
        var snapshotRepo = new SnapshotRepository(_db);
        var userFactRepo = new UserFactRepository(_db);
        var planRepo = new PlanRepository(_db);
        var proposalRepo = new ProposalRepository(_db);
        var rules = TestRulesFactory.CreateConfig();

        _sessionService = new SessionService(sessionRepo);
        _planService = new PlanService(rules, planRepo, snapshotRepo, userFactRepo, _sessionService);
        _proposalService = new ProposalService(proposalRepo);
        _mergeService = new PlanMergeService(rules, planRepo, proposalRepo);

        SeedData(sessionRepo, snapshotRepo).GetAwaiter().GetResult();
    }

    private async Task SeedData(SessionRepository sessionRepo, SnapshotRepository snapshotRepo)
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = SessionStatus.SCANNED,
            AppVersion = "1.0.0-test",
        };
        await sessionRepo.CreateAsync(session);
        _sessionId = session.Id;

        var snapshot = new InventorySnapshot
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            CreatedAt = DateTime.UtcNow,
            Summary = new SnapshotSummary { Drivers = 2, Services = 0, Packages = 0, Apps = 0 },
            Items =
            [
                new InventoryItem
                {
                    ItemId = "drv:removable-item",
                    ItemType = InventoryItemType.DRIVER,
                    DisplayName = "Removable Driver",
                    Vendor = "ThirdParty",
                    Present = false,
                    Signature = new SignatureInfo(),
                },
                new InventoryItem
                {
                    ItemId = "drv:ms-blocked",
                    ItemType = InventoryItemType.DRIVER,
                    DisplayName = "MS Blocked Driver",
                    Vendor = "Microsoft",
                    Present = true,
                    Signature = new SignatureInfo { IsMicrosoft = true, Signed = true },
                },
            ],
        };
        await snapshotRepo.CreateAsync(snapshot);
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task MergeProposalAsync_ScoreDelta_AppliesClamped()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var proposal = await _proposalService.CreateAsync(_sessionId, "Adjust score", new List<ProposalChange>
        {
            new() { Type = "score_delta", TargetId = "drv:removable-item", Delta = 10, Reason = "Evidence found" },
        });
        await _proposalService.ApproveAsync(proposal.Id);

        var result = await _mergeService.MergeProposalAsync(_sessionId, proposal.Id);

        Assert.Single(result.Applied);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public async Task MergeProposalAsync_LargeDelta_GetsClamped()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var proposal = await _proposalService.CreateAsync(_sessionId, "Big change", new List<ProposalChange>
        {
            new() { Type = "score_delta", TargetId = "drv:removable-item", Delta = 50, Reason = "Excessive delta" },
        });
        await _proposalService.ApproveAsync(proposal.Id);

        var result = await _mergeService.MergeProposalAsync(_sessionId, proposal.Id);

        // Should be clamped to ±25 without user fact
        Assert.Single(result.Applied);
        Assert.Contains("clamped", result.Applied[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MergeProposalAsync_HardBlockedItem_SkipsScoreDelta()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var proposal = await _proposalService.CreateAsync(_sessionId, "Modify blocked", new List<ProposalChange>
        {
            new() { Type = "score_delta", TargetId = "drv:ms-blocked", Delta = 10, Reason = "Try to modify" },
        });
        await _proposalService.ApproveAsync(proposal.Id);

        var result = await _mergeService.MergeProposalAsync(_sessionId, proposal.Id);

        Assert.Empty(result.Applied);
        Assert.Single(result.Skipped);
        Assert.Contains("hard-blocked", result.Skipped[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MergeProposalAsync_NoteAdd_Applies()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var proposal = await _proposalService.CreateAsync(_sessionId, "Add note", new List<ProposalChange>
        {
            new() { Type = "note_add", TargetId = "drv:removable-item", Note = "User confirmed", Reason = "Verified" },
        });
        await _proposalService.ApproveAsync(proposal.Id);

        var result = await _mergeService.MergeProposalAsync(_sessionId, proposal.Id);

        Assert.Single(result.Applied);
    }

    [Fact]
    public async Task MergeProposalAsync_PinProtect_SetsKeep()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var proposal = await _proposalService.CreateAsync(_sessionId, "Pin item", new List<ProposalChange>
        {
            new() { Type = "pin_protect", TargetId = "drv:removable-item", Reason = "User wants to keep" },
        });
        await _proposalService.ApproveAsync(proposal.Id);

        var result = await _mergeService.MergeProposalAsync(_sessionId, proposal.Id);

        Assert.Single(result.Applied);
    }

    [Fact]
    public async Task MergeProposalAsync_PendingProposal_Throws()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var proposal = await _proposalService.CreateAsync(_sessionId, "Not approved", new List<ProposalChange>
        {
            new() { Type = "note_add", TargetId = "drv:removable-item", Reason = "test" },
        });

        // Do not approve
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mergeService.MergeProposalAsync(_sessionId, proposal.Id));
    }

    [Fact]
    public async Task MergeProposalAsync_UnknownTarget_Skipped()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var proposal = await _proposalService.CreateAsync(_sessionId, "Bad target", new List<ProposalChange>
        {
            new() { Type = "note_add", TargetId = "drv:nonexistent", Reason = "test" },
        });
        await _proposalService.ApproveAsync(proposal.Id);

        var result = await _mergeService.MergeProposalAsync(_sessionId, proposal.Id);

        Assert.Empty(result.Applied);
        Assert.Single(result.Skipped);
    }
}
