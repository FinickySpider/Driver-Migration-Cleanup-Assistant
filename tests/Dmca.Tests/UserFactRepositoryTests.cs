using Dmca.Core.Models;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="UserFactRepository"/> against in-memory SQLite.
/// Verifies append-only semantics.
/// </summary>
public sealed class UserFactRepositoryTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly UserFactRepository _sut;
    private readonly SessionRepository _sessionRepo;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;

    public UserFactRepositoryTests()
    {
        _db = DmcaDbContext.InMemory($"fact_repo_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();
        _sut = new UserFactRepository(_db);
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

    private static UserFact MakeFact(Guid sessionId, string key = "old_platform_vendor", string value = "Intel") => new()
    {
        SessionId = sessionId,
        Key = key,
        Value = value,
        Source = FactSource.USER,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task Add_ThenGetBySession_ReturnsFact()
    {
        var sessionId = await CreateSessionAsync();
        var fact = MakeFact(sessionId);

        await _sut.AddAsync(fact);
        var facts = await _sut.GetBySessionIdAsync(sessionId);

        Assert.Single(facts);
        Assert.Equal("old_platform_vendor", facts[0].Key);
        Assert.Equal("Intel", facts[0].Value);
        Assert.Equal(FactSource.USER, facts[0].Source);
    }

    [Fact]
    public async Task AddRange_ThenGetBySession_ReturnsAll()
    {
        var sessionId = await CreateSessionAsync();
        var facts = new[]
        {
            MakeFact(sessionId, "old_platform_vendor", "Intel"),
            MakeFact(sessionId, "new_platform_vendor", "AMD"),
            MakeFact(sessionId, "reason", "Upgraded motherboard"),
        };

        await _sut.AddRangeAsync(facts);
        var loaded = await _sut.GetBySessionIdAsync(sessionId);

        Assert.Equal(3, loaded.Count);
    }

    [Fact]
    public async Task GetBySession_DifferentSession_ReturnsEmpty()
    {
        var sessionId = await CreateSessionAsync();
        await _sut.AddAsync(MakeFact(sessionId));

        var otherSessionId = await CreateSessionAsync();
        var facts = await _sut.GetBySessionIdAsync(otherSessionId);

        Assert.Empty(facts);
    }

    [Fact]
    public async Task GetBySession_NoFacts_ReturnsEmpty()
    {
        var sessionId = await CreateSessionAsync();
        var facts = await _sut.GetBySessionIdAsync(sessionId);

        Assert.Empty(facts);
    }
}
