using Dmca.Core.Models;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="PlanService"/> plan generation.
/// </summary>
public sealed class PlanServiceTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
    private readonly SessionService _sessionService;
    private readonly PlanService _planService;

    public PlanServiceTests()
    {
        _db = DmcaDbContext.InMemory($"plan_test_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();

        var sessionRepo = new SessionRepository(_db);
        var snapshotRepo = new SnapshotRepository(_db);
        var userFactRepo = new UserFactRepository(_db);
        var planRepo = new PlanRepository(_db);
        var rules = TestRulesFactory.CreateConfig();

        _sessionService = new SessionService(sessionRepo);
        _planService = new PlanService(rules, planRepo, snapshotRepo, userFactRepo, _sessionService);

        // Seed session + snapshot
        SeedData(sessionRepo, snapshotRepo).GetAwaiter().GetResult();
    }

    private Guid _sessionId;

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
                    ItemId = "drv:intel-mei",
                    ItemType = InventoryItemType.DRIVER,
                    DisplayName = "Intel Management Engine Interface",
                    Vendor = "Intel Corporation",
                    Present = false,
                    Signature = new SignatureInfo { Signed = true, Signer = "Intel", IsMicrosoft = false },
                },
                new InventoryItem
                {
                    ItemId = "drv:ms-storage",
                    ItemType = InventoryItemType.DRIVER,
                    DisplayName = "Microsoft Storage Spaces Controller",
                    Vendor = "Microsoft",
                    Present = true,
                    Signature = new SignatureInfo { Signed = true, IsMicrosoft = true, Signer = "Microsoft Windows" },
                },
            ],
        };
        await snapshotRepo.CreateAsync(snapshot);
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task GeneratePlanAsync_CreatesPlanWithCorrectItems()
    {
        var plan = await _planService.GeneratePlanAsync(_sessionId);

        Assert.NotNull(plan);
        Assert.Equal(_sessionId, plan.SessionId);
        Assert.Equal(2, plan.Items.Count);
    }

    [Fact]
    public async Task GeneratePlanAsync_NonPresentItem_GetsPositiveScore()
    {
        var plan = await _planService.GeneratePlanAsync(_sessionId);

        var intelItem = plan.Items.First(i => i.ItemId == "drv:intel-mei");
        Assert.True(intelItem.BaselineScore > 0);
        Assert.NotEqual(Recommendation.BLOCKED, intelItem.Recommendation);
    }

    [Fact]
    public async Task GeneratePlanAsync_MicrosoftItem_IsBlocked()
    {
        var plan = await _planService.GeneratePlanAsync(_sessionId);

        var msItem = plan.Items.First(i => i.ItemId == "drv:ms-storage");
        Assert.Equal(Recommendation.BLOCKED, msItem.Recommendation);
        Assert.NotEmpty(msItem.HardBlocks);
        Assert.NotNull(msItem.BlockedReason);
    }

    [Fact]
    public async Task GeneratePlanAsync_TransitionsSessionToPlanned()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var session = await _sessionService.GetSessionAsync(_sessionId);
        Assert.Equal(SessionStatus.PLANNED, session!.Status);
    }

    [Fact]
    public async Task GetCurrentPlanAsync_ReturnsPlan()
    {
        var created = await _planService.GeneratePlanAsync(_sessionId);

        var retrieved = await _planService.GetCurrentPlanAsync(_sessionId);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetHardBlocksForItemAsync_ReturnsBlocks()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var blocks = await _planService.GetHardBlocksForItemAsync(_sessionId, "drv:ms-storage");
        Assert.NotEmpty(blocks);
    }

    [Fact]
    public async Task GetHardBlocksForItemAsync_NoBlocks_ReturnsEmpty()
    {
        await _planService.GeneratePlanAsync(_sessionId);

        var blocks = await _planService.GetHardBlocksForItemAsync(_sessionId, "drv:intel-mei");
        Assert.Empty(blocks);
    }
}
