using Dmca.Core.Execution;
using Dmca.Core.Execution.Actions;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Data;
using Dmca.Data.Repositories;
using Xunit;

namespace Dmca.Tests;

public sealed class ExecutionEngineTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
    private readonly IActionQueueRepository _queueRepo;
    private readonly IAuditLogRepository _auditLogRepo;

    public ExecutionEngineTests()
    {
        _db = DmcaDbContext.InMemory($"ee_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();
        _queueRepo = new ActionQueueRepository(_db);
        _auditLogRepo = new AuditLogRepository(_db);
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public async Task Execute_DryRun_Does_Not_Run_Commands()
    {
        var sessionId = await SeedSession();
        var queue = CreateQueue(sessionId, ExecutionMode.DRY_RUN,
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"),
            MakeAction(1, ActionType.UNINSTALL_DRIVER_PACKAGE, "drv:oem1.inf"));

        await _queueRepo.CreateAsync(queue);

        var handlers = new IActionHandler[] { new FakeHandler(ActionType.CREATE_RESTORE_POINT), new FakeHandler(ActionType.UNINSTALL_DRIVER_PACKAGE) };
        var auditLogger = new AuditLogger(_auditLogRepo);
        var engine = new ExecutionEngine(_queueRepo, auditLogger, handlers);

        var result = await engine.ExecuteAsync(queue.Id);

        Assert.Equal(ActionStatus.DRY_RUN, result.OverallStatus);
        Assert.All(result.Actions, a => Assert.Equal(ActionStatus.DRY_RUN, a.Status));
    }

    [Fact]
    public async Task Execute_Live_Completes_All_Actions()
    {
        var sessionId = await SeedSession();
        var queue = CreateQueue(sessionId, ExecutionMode.LIVE,
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"),
            MakeAction(1, ActionType.DISABLE_SERVICE, "svc:TestService"));

        await _queueRepo.CreateAsync(queue);

        var handlers = new IActionHandler[] { new FakeHandler(ActionType.CREATE_RESTORE_POINT), new FakeHandler(ActionType.DISABLE_SERVICE) };
        var auditLogger = new AuditLogger(_auditLogRepo);
        var engine = new ExecutionEngine(_queueRepo, auditLogger, handlers);

        var result = await engine.ExecuteAsync(queue.Id);

        Assert.Equal(ActionStatus.COMPLETED, result.OverallStatus);
        Assert.All(result.Actions, a => Assert.Equal(ActionStatus.COMPLETED, a.Status));
    }

    [Fact]
    public async Task Execute_RestorePoint_Failure_Cancels_Remaining()
    {
        var sessionId = await SeedSession();
        var queue = CreateQueue(sessionId, ExecutionMode.LIVE,
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"),
            MakeAction(1, ActionType.UNINSTALL_DRIVER_PACKAGE, "drv:oem1.inf"),
            MakeAction(2, ActionType.DISABLE_SERVICE, "svc:TestService"));

        await _queueRepo.CreateAsync(queue);

        var handlers = new IActionHandler[]
        {
            new FakeHandler(ActionType.CREATE_RESTORE_POINT, shouldFail: true),
            new FakeHandler(ActionType.UNINSTALL_DRIVER_PACKAGE),
            new FakeHandler(ActionType.DISABLE_SERVICE),
        };
        var auditLogger = new AuditLogger(_auditLogRepo);
        var engine = new ExecutionEngine(_queueRepo, auditLogger, handlers);

        var result = await engine.ExecuteAsync(queue.Id);

        Assert.Equal(ActionStatus.FAILED, result.OverallStatus);
        Assert.Equal(ActionStatus.FAILED, result.Actions[0].Status);
        Assert.Equal(ActionStatus.CANCELLED, result.Actions[1].Status);
        Assert.Equal(ActionStatus.CANCELLED, result.Actions[2].Status);
    }

    [Fact]
    public async Task Execute_Cancellation_Cancels_Remaining_Actions()
    {
        var sessionId = await SeedSession();
        var queue = CreateQueue(sessionId, ExecutionMode.LIVE,
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"),
            MakeAction(1, ActionType.UNINSTALL_DRIVER_PACKAGE, "drv:oem1.inf"));

        await _queueRepo.CreateAsync(queue);

        using var cts = new CancellationTokenSource();

        // Use a handler that cancels after the first action
        var cancellingHandler = new CancelAfterHandler(ActionType.CREATE_RESTORE_POINT, cts);
        var handlers = new IActionHandler[] { cancellingHandler, new FakeHandler(ActionType.UNINSTALL_DRIVER_PACKAGE) };
        var auditLogger = new AuditLogger(_auditLogRepo);
        var engine = new ExecutionEngine(_queueRepo, auditLogger, handlers);

        var result = await engine.ExecuteAsync(queue.Id, cts.Token);

        Assert.Equal(ActionStatus.CANCELLED, result.OverallStatus);
    }

    [Fact]
    public async Task Execute_Creates_Audit_Entries()
    {
        var sessionId = await SeedSession();
        var queue = CreateQueue(sessionId, ExecutionMode.LIVE,
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"));

        await _queueRepo.CreateAsync(queue);

        var handlers = new IActionHandler[] { new FakeHandler(ActionType.CREATE_RESTORE_POINT) };
        var auditLogger = new AuditLogger(_auditLogRepo);
        var engine = new ExecutionEngine(_queueRepo, auditLogger, handlers);

        await engine.ExecuteAsync(queue.Id);

        var entries = await _auditLogRepo.GetBySessionIdAsync(sessionId);
        Assert.True(entries.Count >= 2); // At least start + completed
        Assert.Contains(entries, e => e.Status == ActionStatus.RUNNING);
        Assert.Contains(entries, e => e.Status == ActionStatus.COMPLETED);
    }

    [Fact]
    public async Task Execute_Fires_Progress_Events()
    {
        var sessionId = await SeedSession();
        var queue = CreateQueue(sessionId, ExecutionMode.LIVE,
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"));

        await _queueRepo.CreateAsync(queue);

        var handlers = new IActionHandler[] { new FakeHandler(ActionType.CREATE_RESTORE_POINT) };
        var auditLogger = new AuditLogger(_auditLogRepo);
        var engine = new ExecutionEngine(_queueRepo, auditLogger, handlers);

        var startEvents = new List<ActionProgressEventArgs>();
        var completeEvents = new List<ActionProgressEventArgs>();
        engine.ActionStarting += (_, e) => startEvents.Add(e);
        engine.ActionCompleted += (_, e) => completeEvents.Add(e);

        await engine.ExecuteAsync(queue.Id);

        Assert.Single(startEvents);
        Assert.Single(completeEvents);
        Assert.Equal(1, startEvents[0].Current);
        Assert.Equal(1, startEvents[0].Total);
    }

    [Fact]
    public async Task Execute_Rejects_NonPending_Queue()
    {
        var sessionId = await SeedSession();
        var queue = CreateQueue(sessionId, ExecutionMode.LIVE,
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"));

        await _queueRepo.CreateAsync(queue);
        await _queueRepo.UpdateQueueStatusAsync(queue.Id, ActionStatus.COMPLETED);

        var handlers = new IActionHandler[] { new FakeHandler(ActionType.CREATE_RESTORE_POINT) };
        var auditLogger = new AuditLogger(_auditLogRepo);
        var engine = new ExecutionEngine(_queueRepo, auditLogger, handlers);

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.ExecuteAsync(queue.Id));
    }

    [Fact]
    public async Task Execute_Persists_Updated_Actions()
    {
        var sessionId = await SeedSession();
        var queue = CreateQueue(sessionId, ExecutionMode.LIVE,
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"));

        await _queueRepo.CreateAsync(queue);

        var handlers = new IActionHandler[] { new FakeHandler(ActionType.CREATE_RESTORE_POINT) };
        var auditLogger = new AuditLogger(_auditLogRepo);
        var engine = new ExecutionEngine(_queueRepo, auditLogger, handlers);

        await engine.ExecuteAsync(queue.Id);

        var loaded = await _queueRepo.GetByIdAsync(queue.Id);
        Assert.NotNull(loaded);
        Assert.Equal(ActionStatus.COMPLETED, loaded.OverallStatus);
        Assert.Equal(ActionStatus.COMPLETED, loaded.Actions[0].Status);
        Assert.NotNull(loaded.Actions[0].StartedAt);
        Assert.NotNull(loaded.Actions[0].CompletedAt);
    }

    // --- Helpers ---

    private async Task<Guid> SeedSession()
    {
        var id = Guid.NewGuid();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO sessions (id, created_at, updated_at, status, app_version) VALUES (@id, @now, @now, 'PLANNED', '1.0')";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
        return id;
    }

    private static ActionQueue CreateQueue(Guid sessionId, ExecutionMode mode, params ExecutionAction[] actions) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = sessionId,
        CreatedAt = DateTime.UtcNow,
        Mode = mode,
        OverallStatus = ActionStatus.PENDING,
        Actions = [.. actions],
    };

    private static ExecutionAction MakeAction(int order, ActionType type, string targetId) => new()
    {
        Id = Guid.NewGuid(),
        Order = order,
        ActionType = type,
        TargetId = targetId,
        DisplayName = $"{type}: {targetId}",
    };
}

/// <summary>
/// Fake handler that always succeeds (or fails if configured).
/// </summary>
file sealed class FakeHandler : IActionHandler
{
    private readonly bool _shouldFail;

    public FakeHandler(ActionType handledType, bool shouldFail = false)
    {
        HandledType = handledType;
        _shouldFail = shouldFail;
    }

    public ActionType HandledType { get; }

    public Task<ActionResult> ExecuteAsync(ExecutionAction action, ExecutionMode mode, CancellationToken cancellationToken = default)
    {
        if (mode == ExecutionMode.DRY_RUN)
            return Task.FromResult(new ActionResult(Success: true, Command: "fake-cmd", Output: "[DRY RUN]"));

        return _shouldFail
            ? Task.FromResult(new ActionResult(Success: false, Command: "fake-cmd", ErrorMessage: "Simulated failure"))
            : Task.FromResult(new ActionResult(Success: true, Command: "fake-cmd", Output: "OK"));
    }
}

/// <summary>
/// Handler that triggers cancellation after executing.
/// </summary>
file sealed class CancelAfterHandler : IActionHandler
{
    private readonly CancellationTokenSource _cts;

    public CancelAfterHandler(ActionType handledType, CancellationTokenSource cts)
    {
        HandledType = handledType;
        _cts = cts;
    }

    public ActionType HandledType { get; }

    public Task<ActionResult> ExecuteAsync(ExecutionAction action, ExecutionMode mode, CancellationToken cancellationToken = default)
    {
        _cts.Cancel();
        return Task.FromResult(new ActionResult(Success: true, Command: "fake-cmd", Output: "OK"));
    }
}
