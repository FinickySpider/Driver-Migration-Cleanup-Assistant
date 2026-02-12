using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="SessionService"/> using in-memory SQLite.
/// </summary>
public sealed class SessionServiceTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly SessionService _sut;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;

    public SessionServiceTests()
    {
        _db = DmcaDbContext.InMemory($"session_tests_{Guid.NewGuid():N}");
        // Keep a connection alive so the in-memory DB persists
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();
        ISessionRepository repo = new SessionRepository(_db);
        _sut = new SessionService(repo);
    }

    public void Dispose()
    {
        _keepAlive.Dispose();
    }

    [Fact]
    public async Task CreateSession_ReturnsNewSession()
    {
        var session = await _sut.CreateSessionAsync();

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(SessionStatus.NEW, session.Status);
        Assert.Equal("1.0.0", session.AppVersion);
    }

    [Fact]
    public async Task GetSession_AfterCreate_ReturnsIt()
    {
        var created = await _sut.CreateSessionAsync();
        var retrieved = await _sut.GetSessionAsync(created.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal(SessionStatus.NEW, retrieved.Status);
    }

    [Fact]
    public async Task GetCurrentSession_ReturnsMostRecent()
    {
        var first = await _sut.CreateSessionAsync();
        await Task.Delay(10); // ensure different CreatedAt
        var second = await _sut.CreateSessionAsync();

        var current = await _sut.GetCurrentSessionAsync();

        Assert.NotNull(current);
        Assert.Equal(second.Id, current.Id);
    }

    [Fact]
    public async Task Transition_ValidTransition_UpdatesStatus()
    {
        var session = await _sut.CreateSessionAsync();

        await _sut.TransitionAsync(session.Id, SessionStatus.SCANNED);

        var updated = await _sut.GetSessionAsync(session.Id);
        Assert.NotNull(updated);
        Assert.Equal(SessionStatus.SCANNED, updated.Status);
    }

    [Fact]
    public async Task Transition_InvalidTransition_Throws()
    {
        var session = await _sut.CreateSessionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.TransitionAsync(session.Id, SessionStatus.COMPLETED));
    }

    [Fact]
    public async Task Transition_NonExistentSession_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.TransitionAsync(Guid.NewGuid(), SessionStatus.SCANNED));
    }
}
