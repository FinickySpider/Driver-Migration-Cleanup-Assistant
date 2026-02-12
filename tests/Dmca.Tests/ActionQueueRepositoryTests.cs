using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Data;
using Dmca.Data.Repositories;
using Xunit;

namespace Dmca.Tests;

public sealed class ActionQueueRepositoryTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
    private readonly IActionQueueRepository _repo;
    private readonly Guid _sessionId = Guid.NewGuid();

    public ActionQueueRepositoryTests()
    {
        _db = DmcaDbContext.InMemory($"aqr_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();
        _repo = new ActionQueueRepository(_db);
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

    private ActionQueue CreateTestQueue(params ExecutionAction[] actions) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = _sessionId,
        CreatedAt = DateTime.UtcNow,
        Mode = ExecutionMode.LIVE,
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

    [Fact]
    public async Task CreateAndGetById_RoundTrips()
    {
        var queue = CreateTestQueue(
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"),
            MakeAction(1, ActionType.UNINSTALL_DRIVER_PACKAGE, "drv:oem1.inf"));

        await _repo.CreateAsync(queue);
        var loaded = await _repo.GetByIdAsync(queue.Id);

        Assert.NotNull(loaded);
        Assert.Equal(queue.Id, loaded.Id);
        Assert.Equal(queue.SessionId, loaded.SessionId);
        Assert.Equal(queue.Mode, loaded.Mode);
        Assert.Equal(queue.OverallStatus, loaded.OverallStatus);
        Assert.Equal(2, loaded.Actions.Count);
        Assert.Equal(ActionType.CREATE_RESTORE_POINT, loaded.Actions[0].ActionType);
        Assert.Equal(ActionType.UNINSTALL_DRIVER_PACKAGE, loaded.Actions[1].ActionType);
    }

    [Fact]
    public async Task GetBySessionId_Returns_Queue()
    {
        var queue = CreateTestQueue(MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"));
        await _repo.CreateAsync(queue);

        var loaded = await _repo.GetBySessionIdAsync(_sessionId);
        Assert.NotNull(loaded);
        Assert.Equal(queue.Id, loaded.Id);
    }

    [Fact]
    public async Task GetById_Returns_Null_For_Unknown_Id()
    {
        var loaded = await _repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(loaded);
    }

    [Fact]
    public async Task UpdateAction_Persists_Changes()
    {
        var action = MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test");
        var queue = CreateTestQueue(action);
        await _repo.CreateAsync(queue);

        action.Status = ActionStatus.COMPLETED;
        action.Command = "test-cmd";
        action.Output = "OK";
        action.StartedAt = DateTime.UtcNow.AddSeconds(-5);
        action.CompletedAt = DateTime.UtcNow;
        await _repo.UpdateActionAsync(action);

        var loaded = await _repo.GetByIdAsync(queue.Id);
        Assert.NotNull(loaded);
        var loadedAction = loaded.Actions[0];
        Assert.Equal(ActionStatus.COMPLETED, loadedAction.Status);
        Assert.Equal("test-cmd", loadedAction.Command);
        Assert.Equal("OK", loadedAction.Output);
        Assert.NotNull(loadedAction.StartedAt);
        Assert.NotNull(loadedAction.CompletedAt);
    }

    [Fact]
    public async Task UpdateQueueStatus_Persists()
    {
        var queue = CreateTestQueue(MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"));
        await _repo.CreateAsync(queue);

        await _repo.UpdateQueueStatusAsync(queue.Id, ActionStatus.COMPLETED);

        var loaded = await _repo.GetByIdAsync(queue.Id);
        Assert.NotNull(loaded);
        Assert.Equal(ActionStatus.COMPLETED, loaded.OverallStatus);
    }

    [Fact]
    public async Task Actions_Preserve_Order()
    {
        var queue = CreateTestQueue(
            MakeAction(2, ActionType.DISABLE_SERVICE, "svc:B"),
            MakeAction(0, ActionType.CREATE_RESTORE_POINT, "session:test"),
            MakeAction(1, ActionType.UNINSTALL_DRIVER_PACKAGE, "drv:A"));

        await _repo.CreateAsync(queue);
        var loaded = await _repo.GetByIdAsync(queue.Id);

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.Actions.Count);
        // Loaded should be ordered by order_num
        Assert.Equal(0, loaded.Actions[0].Order);
        Assert.Equal(1, loaded.Actions[1].Order);
        Assert.Equal(2, loaded.Actions[2].Order);
    }
}
