using Dmca.Core.Models;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="SessionRepository"/> against in-memory SQLite.
/// </summary>
public sealed class SessionRepositoryTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly SessionRepository _sut;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;

    public SessionRepositoryTests()
    {
        _db = DmcaDbContext.InMemory($"session_repo_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();
        _sut = new SessionRepository(_db);
    }

    public void Dispose() => _keepAlive.Dispose();

    private static Session MakeSession(SessionStatus status = SessionStatus.NEW) => new()
    {
        Id = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Status = status,
        AppVersion = "1.0.0",
    };

    [Fact]
    public async Task CreateAndGet_RoundTrips()
    {
        var session = MakeSession();
        await _sut.CreateAsync(session);

        var loaded = await _sut.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);
        Assert.Equal(session.Id, loaded.Id);
        Assert.Equal(session.Status, loaded.Status);
        Assert.Equal(session.AppVersion, loaded.AppVersion);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrent_ReturnsLatest()
    {
        var s1 = MakeSession();
        await _sut.CreateAsync(s1);

        await Task.Delay(10);
        var s2 = MakeSession();
        await _sut.CreateAsync(s2);

        var current = await _sut.GetCurrentAsync();
        Assert.NotNull(current);
        Assert.Equal(s2.Id, current.Id);
    }

    [Fact]
    public async Task Update_ChangesStatus()
    {
        var session = MakeSession();
        await _sut.CreateAsync(session);

        session.Status = SessionStatus.SCANNED;
        session.UpdatedAt = DateTime.UtcNow;
        await _sut.UpdateAsync(session);

        var loaded = await _sut.GetByIdAsync(session.Id);
        Assert.NotNull(loaded);
        Assert.Equal(SessionStatus.SCANNED, loaded.Status);
    }

    [Fact]
    public async Task Update_NonExistent_Throws()
    {
        var session = MakeSession();
        // Never created — should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateAsync(session));
    }
}
