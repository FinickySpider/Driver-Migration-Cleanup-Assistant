using Dmca.Core.Models;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="UserFactsService"/> using in-memory SQLite.
/// </summary>
public sealed class UserFactsServiceTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly UserFactsService _sut;
    private readonly SessionRepository _sessionRepo;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;

    public UserFactsServiceTests()
    {
        _db = DmcaDbContext.InMemory($"facts_svc_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();
        _sessionRepo = new SessionRepository(_db);
        var factRepo = new UserFactRepository(_db);
        _sut = new UserFactsService(factRepo);
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

    [Fact]
    public async Task AddFact_ThenGetFacts_ReturnsFact()
    {
        var sessionId = await CreateSessionAsync();

        await _sut.AddFactAsync(sessionId, "old_platform_vendor", "Intel");

        var facts = await _sut.GetFactsAsync(sessionId);
        Assert.Single(facts);
        Assert.Equal("old_platform_vendor", facts[0].Key);
        Assert.Equal("Intel", facts[0].Value);
        Assert.Equal(FactSource.USER, facts[0].Source);
    }

    [Fact]
    public async Task AddFact_WithAISource_SetsCorrectSource()
    {
        var sessionId = await CreateSessionAsync();

        await _sut.AddFactAsync(sessionId, "inferred_vendor", "Realtek", FactSource.AI);

        var facts = await _sut.GetFactsAsync(sessionId);
        Assert.Single(facts);
        Assert.Equal(FactSource.AI, facts[0].Source);
    }

    [Fact]
    public async Task AddFact_EmptyKey_Throws()
    {
        var sessionId = await CreateSessionAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.AddFactAsync(sessionId, "", "value"));
    }

    [Fact]
    public async Task AddFact_WhitespaceKey_Throws()
    {
        var sessionId = await CreateSessionAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.AddFactAsync(sessionId, "   ", "value"));
    }

    [Fact]
    public async Task GetFacts_EmptySession_ReturnsEmpty()
    {
        var sessionId = await CreateSessionAsync();

        var facts = await _sut.GetFactsAsync(sessionId);
        Assert.Empty(facts);
    }

    [Fact]
    public async Task AddMultipleFacts_AllPersisted()
    {
        var sessionId = await CreateSessionAsync();

        await _sut.AddFactAsync(sessionId, "old_platform_vendor", "Intel");
        await _sut.AddFactAsync(sessionId, "new_platform_vendor", "AMD");
        await _sut.AddFactAsync(sessionId, "migration_reason", "Upgraded motherboard");

        var facts = await _sut.GetFactsAsync(sessionId);
        Assert.Equal(3, facts.Count);
    }
}
