using Dmca.Core.Execution;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Data;
using Dmca.Data.Repositories;
using Xunit;

namespace Dmca.Tests;

public sealed class AuditLoggerTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
    private readonly IAuditLogRepository _auditRepo;
    private readonly AuditLogger _logger;
    private readonly Guid _sessionId = Guid.NewGuid();

    public AuditLoggerTests()
    {
        _db = DmcaDbContext.InMemory($"al_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();
        _auditRepo = new AuditLogRepository(_db);
        _logger = new AuditLogger(_auditRepo);
        SeedSession();
    }

    public void Dispose() => _keepAlive.Dispose();

    private void SeedSession()
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO sessions (id, created_at, updated_at, status, app_version) VALUES (@id, @now, @now, 'PLANNED', '1.0')";
        cmd.Parameters.AddWithValue("@id", _sessionId.ToString());
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private static ExecutionAction MakeAction() => new()
    {
        Id = Guid.NewGuid(),
        Order = 0,
        ActionType = ActionType.CREATE_RESTORE_POINT,
        TargetId = "session:test",
        DisplayName = "Test action",
    };

    [Fact]
    public async Task LogStart_Creates_Running_Entry()
    {
        var action = MakeAction();
        await _logger.LogStartAsync(_sessionId, action);

        var entries = await _logger.GetBySessionAsync(_sessionId);
        Assert.Single(entries);
        Assert.Equal(ActionStatus.RUNNING, entries[0].Status);
        Assert.Equal(action.Id, entries[0].ActionId);
    }

    [Fact]
    public async Task LogCompleted_Creates_Completed_Entry()
    {
        var action = MakeAction();
        await _logger.LogCompletedAsync(_sessionId, action, "output");

        var entries = await _logger.GetBySessionAsync(_sessionId);
        Assert.Single(entries);
        Assert.Equal(ActionStatus.COMPLETED, entries[0].Status);
        Assert.Equal("output", entries[0].Output);
    }

    [Fact]
    public async Task LogFailed_Creates_Failed_Entry_With_Error()
    {
        var action = MakeAction();
        await _logger.LogFailedAsync(_sessionId, action, "something went wrong", "partial output");

        var entries = await _logger.GetBySessionAsync(_sessionId);
        Assert.Single(entries);
        Assert.Equal(ActionStatus.FAILED, entries[0].Status);
        Assert.Equal("something went wrong", entries[0].ErrorMessage);
        Assert.Equal("partial output", entries[0].Output);
    }

    [Fact]
    public async Task LogDryRun_Creates_DryRun_Entry()
    {
        var action = MakeAction();
        await _logger.LogDryRunAsync(_sessionId, action, "some-command");

        var entries = await _logger.GetBySessionAsync(_sessionId);
        Assert.Single(entries);
        Assert.Equal(ActionStatus.DRY_RUN, entries[0].Status);
        Assert.Contains("[DRY RUN]", entries[0].Output);
    }

    [Fact]
    public async Task LogCancelled_Creates_Cancelled_Entry()
    {
        var action = MakeAction();
        await _logger.LogCancelledAsync(_sessionId, action);

        var entries = await _logger.GetBySessionAsync(_sessionId);
        Assert.Single(entries);
        Assert.Equal(ActionStatus.CANCELLED, entries[0].Status);
    }

    [Fact]
    public async Task GetBySession_Returns_All_Entries_In_Order()
    {
        var action = MakeAction();
        await _logger.LogStartAsync(_sessionId, action);
        await _logger.LogCompletedAsync(_sessionId, action, "done");

        var entries = await _logger.GetBySessionAsync(_sessionId);
        Assert.Equal(2, entries.Count);
        Assert.Equal(ActionStatus.RUNNING, entries[0].Status);
        Assert.Equal(ActionStatus.COMPLETED, entries[1].Status);
    }

    [Fact]
    public async Task GetByAction_Returns_Entries_For_Specific_Action()
    {
        var action1 = MakeAction();
        var action2 = MakeAction();
        await _logger.LogStartAsync(_sessionId, action1);
        await _logger.LogStartAsync(_sessionId, action2);

        var entries = await _auditRepo.GetByActionIdAsync(action1.Id);
        Assert.Single(entries);
        Assert.Equal(action1.Id, entries[0].ActionId);
    }
}
