using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="RescanService"/> using mock collectors and in-memory SQLite.
/// </summary>
public sealed class RescanServiceTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly SessionService _sessionService;
    private readonly SnapshotRepository _snapshotRepo;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;

    public RescanServiceTests()
    {
        _db = DmcaDbContext.InMemory($"rescan_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();

        var sessionRepo = new SessionRepository(_db);
        _snapshotRepo = new SnapshotRepository(_db);
        _sessionService = new SessionService(sessionRepo);
    }

    public void Dispose() => _keepAlive.Dispose();

    private RescanService CreateRescanService(
        IEnumerable<IInventoryCollector>? collectors = null,
        IPlatformInfoCollector? platformCollector = null)
    {
        collectors ??= [new MockDriverCollector(), new MockServiceCollector()];
        platformCollector ??= new MockPlatformInfoCollector();
        return new RescanService(collectors, platformCollector, _snapshotRepo, _sessionService);
    }

    /// <summary>
    /// Advances a session through the full lifecycle to EXECUTING so rescan can transition to COMPLETED.
    /// </summary>
    private async Task<Guid> CreateExecutingSessionAsync()
    {
        var session = await _sessionService.CreateSessionAsync();
        await _sessionService.TransitionAsync(session.Id, SessionStatus.SCANNED);
        await _sessionService.TransitionAsync(session.Id, SessionStatus.PLANNED);
        await _sessionService.TransitionAsync(session.Id, SessionStatus.READY_TO_EXECUTE);
        await _sessionService.TransitionAsync(session.Id, SessionStatus.EXECUTING);
        return session.Id;
    }

    [Fact]
    public async Task Rescan_CreatesNewSnapshot()
    {
        var sessionId = await CreateExecutingSessionAsync();
        var sut = CreateRescanService();

        var snapshot = await sut.RescanAsync(sessionId);

        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(sessionId, snapshot.SessionId);
        Assert.True(snapshot.Items.Count > 0);
    }

    [Fact]
    public async Task Rescan_TransitionsSessionToCompleted()
    {
        var sessionId = await CreateExecutingSessionAsync();
        var sut = CreateRescanService();

        await sut.RescanAsync(sessionId);

        var session = await _sessionService.GetSessionAsync(sessionId);
        Assert.NotNull(session);
        Assert.Equal(SessionStatus.COMPLETED, session.Status);
    }

    [Fact]
    public async Task Rescan_PersistsSnapshotToRepository()
    {
        var sessionId = await CreateExecutingSessionAsync();
        var sut = CreateRescanService();

        var snapshot = await sut.RescanAsync(sessionId);

        var loaded = await _snapshotRepo.GetByIdAsync(snapshot.Id);
        Assert.NotNull(loaded);
        Assert.Equal(snapshot.Id, loaded.Id);
    }

    [Fact]
    public async Task Rescan_CollectsFromAllCollectors()
    {
        var sessionId = await CreateExecutingSessionAsync();
        var sut = CreateRescanService();

        var snapshot = await sut.RescanAsync(sessionId);

        // MockDriverCollector returns 2 items, MockServiceCollector returns 1
        Assert.Equal(2, snapshot.Summary.Drivers);
        Assert.Equal(1, snapshot.Summary.Services);
        Assert.Equal(3, snapshot.Items.Count);
    }

    [Fact]
    public async Task Rescan_PopulatesPlatformInfo()
    {
        var sessionId = await CreateExecutingSessionAsync();
        var sut = CreateRescanService();

        var snapshot = await sut.RescanAsync(sessionId);

        Assert.NotNull(snapshot.Summary.Platform);
        Assert.Equal("TestVendor", snapshot.Summary.Platform.MotherboardVendor);
    }

    [Fact]
    public async Task Rescan_ContinuesWhenCollectorFails()
    {
        var sessionId = await CreateExecutingSessionAsync();
        var collectors = new IInventoryCollector[]
        {
            new FailingCollector(),
            new MockDriverCollector(),
        };
        var sut = CreateRescanService(collectors);

        var snapshot = await sut.RescanAsync(sessionId);

        // Only the MockDriverCollector items should be present
        Assert.Equal(2, snapshot.Items.Count);
    }

    [Fact]
    public async Task Rescan_ContinuesWhenPlatformCollectorFails()
    {
        var sessionId = await CreateExecutingSessionAsync();
        var sut = CreateRescanService(platformCollector: new FailingPlatformCollector());

        var snapshot = await sut.RescanAsync(sessionId);

        Assert.Null(snapshot.Summary.Platform);
        Assert.True(snapshot.Items.Count > 0);
    }

    [Fact]
    public async Task Rescan_ProducesDistinctSnapshotId()
    {
        var sessionId = await CreateExecutingSessionAsync();
        // Create an initial snapshot via ScanService pattern
        var initialSnapshot = new InventorySnapshot
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            Summary = new SnapshotSummary { Drivers = 1 },
            Items = new List<InventoryItem>
            {
                new()
                {
                    ItemId = "drv:original.inf",
                    ItemType = InventoryItemType.DRIVER,
                    DisplayName = "Original Driver",
                },
            }.AsReadOnly(),
        };
        await _snapshotRepo.CreateAsync(initialSnapshot);

        var sut = CreateRescanService();
        var rescanSnapshot = await sut.RescanAsync(sessionId);

        Assert.NotEqual(initialSnapshot.Id, rescanSnapshot.Id);
    }

    // ── Mock collectors ──

    private sealed class MockDriverCollector : IInventoryCollector
    {
        public InventoryItemType ItemType => InventoryItemType.DRIVER;

        public Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default)
        {
            var items = new List<InventoryItem>
            {
                new()
                {
                    ItemId = "drv:rescan1.inf",
                    ItemType = InventoryItemType.DRIVER,
                    DisplayName = "Rescan Driver 1",
                    Vendor = "Intel",
                    Present = false,
                },
                new()
                {
                    ItemId = "drv:rescan2.inf",
                    ItemType = InventoryItemType.DRIVER,
                    DisplayName = "Rescan Driver 2",
                    Vendor = "Realtek",
                    Present = true,
                },
            };
            return Task.FromResult<IReadOnlyList<InventoryItem>>(items.AsReadOnly());
        }
    }

    private sealed class MockServiceCollector : IInventoryCollector
    {
        public InventoryItemType ItemType => InventoryItemType.SERVICE;

        public Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default)
        {
            var items = new List<InventoryItem>
            {
                new()
                {
                    ItemId = "svc:RescanSvc",
                    ItemType = InventoryItemType.SERVICE,
                    DisplayName = "Rescan Service",
                    Running = true,
                    StartType = 2,
                },
            };
            return Task.FromResult<IReadOnlyList<InventoryItem>>(items.AsReadOnly());
        }
    }

    private sealed class FailingCollector : IInventoryCollector
    {
        public InventoryItemType ItemType => InventoryItemType.DRIVER;

        public Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default)
        {
            throw new InvalidOperationException("Collector failure simulation");
        }
    }

    private sealed class MockPlatformInfoCollector : IPlatformInfoCollector
    {
        public Task<PlatformInfo> CollectAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new PlatformInfo
            {
                MotherboardVendor = "TestVendor",
                MotherboardProduct = "TestBoard",
                Cpu = "Test CPU",
                OsVersion = "10.0.22631",
            });
        }
    }

    private sealed class FailingPlatformCollector : IPlatformInfoCollector
    {
        public Task<PlatformInfo> CollectAsync(CancellationToken ct = default)
        {
            throw new InvalidOperationException("Platform collector failure simulation");
        }
    }
}
