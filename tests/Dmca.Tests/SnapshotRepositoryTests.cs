using Dmca.Core.Models;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="SnapshotRepository"/> against in-memory SQLite.
/// Verifies create-only (immutable) semantics and item round-tripping.
/// </summary>
public sealed class SnapshotRepositoryTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly SnapshotRepository _sut;
    private readonly SessionRepository _sessionRepo;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;

    public SnapshotRepositoryTests()
    {
        _db = DmcaDbContext.InMemory($"snap_repo_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();
        _sut = new SnapshotRepository(_db);
        _sessionRepo = new SessionRepository(_db);
    }

    public void Dispose() => _keepAlive.Dispose();

    private async Task<Guid> CreateSessionAsync()
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = SessionStatus.NEW,
            AppVersion = "1.0.0",
        };
        await _sessionRepo.CreateAsync(session);
        return session.Id;
    }

    private static InventorySnapshot MakeSnapshot(Guid sessionId, int driverCount = 2)
    {
        var items = new List<InventoryItem>();
        for (var i = 0; i < driverCount; i++)
        {
            items.Add(new InventoryItem
            {
                ItemId = $"drv:test{i}.inf",
                ItemType = InventoryItemType.DRIVER,
                DisplayName = $"Test Driver {i}",
                Vendor = "TestVendor",
                Provider = "TestProvider",
                Version = "1.0.0",
                DriverInf = $"test{i}.inf",
                DeviceHardwareIds = ["PCI\\VEN_8086&DEV_1234"],
                Present = i == 0,
                Signature = new SignatureInfo
                {
                    Signed = true,
                    Signer = "Microsoft Windows",
                    IsMicrosoft = true,
                    IsWHQL = true,
                },
                Paths = [$"C:\\Windows\\System32\\drivers\\test{i}.sys"],
            });
        }

        // Add a service item
        items.Add(new InventoryItem
        {
            ItemId = "svc:TestService",
            ItemType = InventoryItemType.SERVICE,
            DisplayName = "Test Service",
            Running = true,
            StartType = 2,
            Dependencies = ["RpcSs", "EventLog"],
        });

        // Add an app item
        items.Add(new InventoryItem
        {
            ItemId = "app:TestApp",
            ItemType = InventoryItemType.APP,
            DisplayName = "Test Application",
            Vendor = "TestPublisher",
            Version = "3.2.1",
            InstallDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        });

        return new InventorySnapshot
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            Summary = new SnapshotSummary
            {
                Drivers = driverCount,
                Services = 1,
                Packages = 0,
                Apps = 1,
                Platform = new PlatformInfo
                {
                    MotherboardVendor = "ASUSTeK",
                    MotherboardProduct = "ROG STRIX B650E-E",
                    Cpu = "AMD Ryzen 9 7950X",
                    OsVersion = "10.0.22631",
                },
            },
            Items = items.AsReadOnly(),
        };
    }

    [Fact]
    public async Task Create_ThenGetById_RoundTrips()
    {
        var sessionId = await CreateSessionAsync();
        var snapshot = MakeSnapshot(sessionId);

        await _sut.CreateAsync(snapshot);
        var loaded = await _sut.GetByIdAsync(snapshot.Id);

        Assert.NotNull(loaded);
        Assert.Equal(snapshot.Id, loaded.Id);
        Assert.Equal(snapshot.SessionId, loaded.SessionId);
        Assert.Equal(snapshot.Items.Count, loaded.Items.Count);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestBySessionId_ReturnsNewest()
    {
        var sessionId = await CreateSessionAsync();

        var snap1 = MakeSnapshot(sessionId, 1);
        await _sut.CreateAsync(snap1);

        await Task.Delay(10);
        var snap2 = MakeSnapshot(sessionId, 3);
        await _sut.CreateAsync(snap2);

        var latest = await _sut.GetLatestBySessionIdAsync(sessionId);

        Assert.NotNull(latest);
        Assert.Equal(snap2.Id, latest.Id);
    }

    [Fact]
    public async Task GetItemsBySnapshotId_ReturnsAllItems()
    {
        var sessionId = await CreateSessionAsync();
        var snapshot = MakeSnapshot(sessionId, 2);

        await _sut.CreateAsync(snapshot);
        var items = await _sut.GetItemsBySnapshotIdAsync(snapshot.Id);

        // 2 drivers + 1 service + 1 app = 4
        Assert.Equal(4, items.Count);
    }

    [Fact]
    public async Task RoundTrips_DriverFields()
    {
        var sessionId = await CreateSessionAsync();
        var snapshot = MakeSnapshot(sessionId);

        await _sut.CreateAsync(snapshot);
        var loaded = await _sut.GetByIdAsync(snapshot.Id);

        Assert.NotNull(loaded);
        var driver = loaded.Items.First(i => i.ItemId == "drv:test0.inf");

        Assert.Equal(InventoryItemType.DRIVER, driver.ItemType);
        Assert.Equal("Test Driver 0", driver.DisplayName);
        Assert.Equal("TestVendor", driver.Vendor);
        Assert.True(driver.Present);
        Assert.NotNull(driver.Signature);
        Assert.True(driver.Signature.Signed);
        Assert.True(driver.Signature.IsMicrosoft);
        Assert.NotNull(driver.DeviceHardwareIds);
        Assert.Contains("PCI\\VEN_8086&DEV_1234", driver.DeviceHardwareIds);
        Assert.NotNull(driver.Paths);
        Assert.Single(driver.Paths);
    }

    [Fact]
    public async Task RoundTrips_ServiceFields()
    {
        var sessionId = await CreateSessionAsync();
        var snapshot = MakeSnapshot(sessionId);

        await _sut.CreateAsync(snapshot);
        var loaded = await _sut.GetByIdAsync(snapshot.Id);

        Assert.NotNull(loaded);
        var svc = loaded.Items.First(i => i.ItemId == "svc:TestService");

        Assert.Equal(InventoryItemType.SERVICE, svc.ItemType);
        Assert.True(svc.Running);
        Assert.Equal(2, svc.StartType);
        Assert.NotNull(svc.Dependencies);
        Assert.Equal(2, svc.Dependencies.Count);
    }

    [Fact]
    public async Task RoundTrips_AppFields()
    {
        var sessionId = await CreateSessionAsync();
        var snapshot = MakeSnapshot(sessionId);

        await _sut.CreateAsync(snapshot);
        var loaded = await _sut.GetByIdAsync(snapshot.Id);

        Assert.NotNull(loaded);
        var app = loaded.Items.First(i => i.ItemId == "app:TestApp");

        Assert.Equal(InventoryItemType.APP, app.ItemType);
        Assert.Equal("TestPublisher", app.Vendor);
        Assert.Equal("3.2.1", app.Version);
        Assert.NotNull(app.InstallDate);
    }

    [Fact]
    public async Task RoundTrips_SummaryAndPlatform()
    {
        var sessionId = await CreateSessionAsync();
        var snapshot = MakeSnapshot(sessionId);

        await _sut.CreateAsync(snapshot);
        var loaded = await _sut.GetByIdAsync(snapshot.Id);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Summary.Drivers);
        Assert.Equal(1, loaded.Summary.Services);
        Assert.Equal(1, loaded.Summary.Apps);
        Assert.NotNull(loaded.Summary.Platform);
        Assert.Equal("ASUSTeK", loaded.Summary.Platform.MotherboardVendor);
        Assert.Equal("AMD Ryzen 9 7950X", loaded.Summary.Platform.Cpu);
    }

    [Fact]
    public async Task GetAllBySessionId_ReturnsAllSnapshotsInOrder()
    {
        var sessionId = await CreateSessionAsync();

        var snap1 = MakeSnapshot(sessionId, 1);
        await _sut.CreateAsync(snap1);

        await Task.Delay(10);
        var snap2 = MakeSnapshot(sessionId, 3);
        await _sut.CreateAsync(snap2);

        var all = await _sut.GetAllBySessionIdAsync(sessionId);

        Assert.Equal(2, all.Count);
        // Ordered by created_at ASC
        Assert.Equal(snap1.Id, all[0].Id);
        Assert.Equal(snap2.Id, all[1].Id);
    }

    [Fact]
    public async Task GetAllBySessionId_NoSnapshots_ReturnsEmpty()
    {
        var sessionId = await CreateSessionAsync();

        var all = await _sut.GetAllBySessionIdAsync(sessionId);

        Assert.Empty(all);
    }

    [Fact]
    public async Task GetAllBySessionId_DoesNotReturnOtherSessions()
    {
        var sessionId1 = await CreateSessionAsync();
        var sessionId2 = await CreateSessionAsync();

        await _sut.CreateAsync(MakeSnapshot(sessionId1, 1));
        await _sut.CreateAsync(MakeSnapshot(sessionId2, 2));

        var all = await _sut.GetAllBySessionIdAsync(sessionId1);

        Assert.Single(all);
        Assert.Equal(sessionId1, all[0].SessionId);
    }
}
