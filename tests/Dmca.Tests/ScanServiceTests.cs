using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="ScanService"/> using mock collectors and in-memory SQLite.
/// </summary>
public sealed class ScanServiceTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly ScanService _sut;
    private readonly SessionService _sessionService;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
    private readonly SnapshotRepository _snapshotRepo;

    public ScanServiceTests()
    {
        _db = DmcaDbContext.InMemory($"scan_svc_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();

        var sessionRepo = new SessionRepository(_db);
        _snapshotRepo = new SnapshotRepository(_db);
        _sessionService = new SessionService(sessionRepo);

        IInventoryCollector[] collectors =
        [
            new MockDriverCollector(),
            new MockServiceCollector(),
            new MockAppCollector(),
        ];

        IPlatformInfoCollector platformCollector = new MockPlatformInfoCollector();

        _sut = new ScanService(collectors, platformCollector, _snapshotRepo, _sessionService);
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task Scan_CreatesSnapshot_WithCorrectCounts()
    {
        var session = await _sessionService.CreateSessionAsync();

        var snapshot = await _sut.ScanAsync(session.Id);

        Assert.Equal(2, snapshot.Summary.Drivers);
        Assert.Equal(1, snapshot.Summary.Services);
        Assert.Equal(1, snapshot.Summary.Apps);
        Assert.Equal(4, snapshot.Items.Count);
    }

    [Fact]
    public async Task Scan_TransitionsSessionToScanned()
    {
        var session = await _sessionService.CreateSessionAsync();

        await _sut.ScanAsync(session.Id);

        var updated = await _sessionService.GetSessionAsync(session.Id);
        Assert.NotNull(updated);
        Assert.Equal(SessionStatus.SCANNED, updated.Status);
    }

    [Fact]
    public async Task Scan_PersistsSnapshot()
    {
        var session = await _sessionService.CreateSessionAsync();

        var snapshot = await _sut.ScanAsync(session.Id);

        var loaded = await _snapshotRepo.GetByIdAsync(snapshot.Id);
        Assert.NotNull(loaded);
        Assert.Equal(snapshot.Items.Count, loaded.Items.Count);
    }

    [Fact]
    public async Task Scan_PopulatesPlatformInfo()
    {
        var session = await _sessionService.CreateSessionAsync();

        var snapshot = await _sut.ScanAsync(session.Id);

        Assert.NotNull(snapshot.Summary.Platform);
        Assert.Equal("TestVendor", snapshot.Summary.Platform.MotherboardVendor);
        Assert.Equal("TestBoard", snapshot.Summary.Platform.MotherboardProduct);
        Assert.Equal("Test CPU", snapshot.Summary.Platform.Cpu);
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
                    ItemId = "drv:mock1.inf",
                    ItemType = InventoryItemType.DRIVER,
                    DisplayName = "Mock Driver 1",
                    Vendor = "Intel",
                    Present = false,
                },
                new()
                {
                    ItemId = "drv:mock2.inf",
                    ItemType = InventoryItemType.DRIVER,
                    DisplayName = "Mock Driver 2",
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
                    ItemId = "svc:MockSvc",
                    ItemType = InventoryItemType.SERVICE,
                    DisplayName = "Mock Service",
                    Running = true,
                    StartType = 2,
                },
            };
            return Task.FromResult<IReadOnlyList<InventoryItem>>(items.AsReadOnly());
        }
    }

    private sealed class MockAppCollector : IInventoryCollector
    {
        public InventoryItemType ItemType => InventoryItemType.APP;

        public Task<IReadOnlyList<InventoryItem>> CollectAsync(CancellationToken ct = default)
        {
            var items = new List<InventoryItem>
            {
                new()
                {
                    ItemId = "app:MockApp",
                    ItemType = InventoryItemType.APP,
                    DisplayName = "Mock App",
                    Vendor = "MockPublisher",
                },
            };
            return Task.FromResult<IReadOnlyList<InventoryItem>>(items.AsReadOnly());
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
}
