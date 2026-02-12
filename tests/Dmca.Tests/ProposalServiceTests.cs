using Dmca.Core.Models;
using Dmca.Core.Services;
using Dmca.Data;
using Dmca.Data.Repositories;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="ProposalService"/> CRUD and lifecycle.
/// </summary>
public sealed class ProposalServiceTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
    private readonly ProposalService _service;
    private readonly Guid _sessionId;

    public ProposalServiceTests()
    {
        _db = DmcaDbContext.InMemory($"proposal_test_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();

        // Seed a session to satisfy FK constraint
        _sessionId = Guid.NewGuid();
        var sessionRepo = new SessionRepository(_db);
        sessionRepo.CreateAsync(new Session
        {
            Id = _sessionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = SessionStatus.PLANNED,
            AppVersion = "1.0.0-test",
        }).GetAwaiter().GetResult();

        var repo = new ProposalRepository(_db);
        _service = new ProposalService(repo);
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task CreateAsync_ValidProposal_ReturnsPending()
    {
        var changes = CreateChanges(1);
        var proposal = await _service.CreateAsync(_sessionId, "Test Proposal", changes);

        Assert.NotEqual(Guid.Empty, proposal.Id);
        Assert.Equal(ProposalStatus.PENDING, proposal.Status);
        Assert.Equal("Test Proposal", proposal.Title);
        Assert.Single(proposal.Changes);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_Throws()
    {
        var changes = CreateChanges(1);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(_sessionId, "", changes));
    }

    [Fact]
    public async Task CreateAsync_NoChanges_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(_sessionId, "Test", []));
    }

    [Fact]
    public async Task CreateAsync_TooManyChanges_Throws()
    {
        var changes = CreateChanges(6);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(_sessionId, "Test", changes));
    }

    [Fact]
    public async Task ApproveAsync_PendingProposal_Succeeds()
    {
        var proposal = await _service.CreateAsync(_sessionId, "Test", CreateChanges(1));
        await _service.ApproveAsync(proposal.Id);

        var updated = await _service.GetByIdAsync(proposal.Id);
        Assert.Equal(ProposalStatus.APPROVED, updated!.Status);
    }

    [Fact]
    public async Task RejectAsync_PendingProposal_Succeeds()
    {
        var proposal = await _service.CreateAsync(_sessionId, "Test", CreateChanges(1));
        await _service.RejectAsync(proposal.Id);

        var updated = await _service.GetByIdAsync(proposal.Id);
        Assert.Equal(ProposalStatus.REJECTED, updated!.Status);
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_Throws()
    {
        var proposal = await _service.CreateAsync(_sessionId, "Test", CreateChanges(1));
        await _service.ApproveAsync(proposal.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApproveAsync(proposal.Id));
    }

    [Fact]
    public async Task ApproveAsync_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ApproveAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListBySessionAsync_ReturnsAll()
    {
        await _service.CreateAsync(_sessionId, "P1", CreateChanges(1));
        await _service.CreateAsync(_sessionId, "P2", CreateChanges(2));

        var proposals = await _service.ListBySessionAsync(_sessionId);
        Assert.Equal(2, proposals.Count);
    }

    [Fact]
    public void ComputeRisk_SmallDelta_Low()
    {
        var changes = new List<ProposalChange>
        {
            new() { Type = "note_add", TargetId = "drv:test", Reason = "test" },
        };

        Assert.Equal(EstimatedRisk.LOW, ProposalService.ComputeRisk(changes));
    }

    [Fact]
    public void ComputeRisk_LargeDelta_High()
    {
        var changes = new List<ProposalChange>
        {
            new() { Type = "score_delta", TargetId = "drv:test", Delta = 20, Reason = "test" },
        };

        Assert.Equal(EstimatedRisk.HIGH, ProposalService.ComputeRisk(changes));
    }

    [Fact]
    public void ComputeRisk_ActionAdd_Medium()
    {
        var changes = new List<ProposalChange>
        {
            new() { Type = "action_add", TargetId = "drv:test", Reason = "test" },
        };

        Assert.Equal(EstimatedRisk.MEDIUM, ProposalService.ComputeRisk(changes));
    }

    private static List<ProposalChange> CreateChanges(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new ProposalChange
            {
                Type = "note_add",
                TargetId = $"drv:item-{i}",
                Reason = $"Test reason {i}",
                Note = $"Test note {i}",
            })
            .ToList();
}
