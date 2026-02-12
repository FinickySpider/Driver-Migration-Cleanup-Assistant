using Dmca.Core.Execution;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Data;
using Dmca.Data.Repositories;
using Xunit;

namespace Dmca.Tests;

public sealed class ActionQueueBuilderTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;
    private readonly IPlanRepository _planRepo;
    private readonly IActionQueueRepository _queueRepo;

    public ActionQueueBuilderTests()
    {
        _db = DmcaDbContext.InMemory($"aqb_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
        _db.EnsureSchema();
        _planRepo = new PlanRepository(_db);
        _queueRepo = new ActionQueueRepository(_db);
    }

    public void Dispose()
    {
        _keepAlive.Dispose();
    }

    private async Task<Guid> SeedSessionAndPlan(params PlanItem[] items)
    {
        var sessionId = Guid.NewGuid();
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO sessions (id, created_at, updated_at, status, app_version) VALUES (@id, @now, @now, 'PLANNED', '1.0')",
            new { id = sessionId.ToString(), now = DateTime.UtcNow.ToString("O") });

        var plan = new DecisionPlan
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            Items = items.ToList(),
        };
        await _planRepo.CreateAsync(plan);
        return sessionId;
    }

    [Fact]
    public async Task Build_Creates_RestorePoint_As_First_Action()
    {
        var sessionId = await SeedSessionAndPlan(
            MakePlanItem("drv:oem42.inf", Recommendation.REMOVE_STAGE_1));

        var builder = new ActionQueueBuilder(_planRepo, _queueRepo);
        var queue = await builder.BuildAsync(sessionId);

        Assert.NotNull(queue);
        Assert.Equal(2, queue.Actions.Count);
        Assert.Equal(ActionType.CREATE_RESTORE_POINT, queue.Actions[0].ActionType);
        Assert.Equal(0, queue.Actions[0].Order);
    }

    [Fact]
    public async Task Build_Includes_RemoveStage1_And_Stage2_Items()
    {
        var sessionId = await SeedSessionAndPlan(
            MakePlanItem("drv:oem1.inf", Recommendation.REMOVE_STAGE_1),
            MakePlanItem("svc:OldService", Recommendation.REMOVE_STAGE_2),
            MakePlanItem("app:SomeApp", Recommendation.REMOVE_STAGE_1));

        var builder = new ActionQueueBuilder(_planRepo, _queueRepo);
        var queue = await builder.BuildAsync(sessionId);

        // 1 restore point + 3 items
        Assert.Equal(4, queue.Actions.Count);
        Assert.Contains(queue.Actions, a => a.ActionType == ActionType.UNINSTALL_DRIVER_PACKAGE && a.TargetId == "drv:oem1.inf");
        Assert.Contains(queue.Actions, a => a.ActionType == ActionType.DISABLE_SERVICE && a.TargetId == "svc:OldService");
        Assert.Contains(queue.Actions, a => a.ActionType == ActionType.UNINSTALL_PROGRAM && a.TargetId == "app:SomeApp");
    }

    [Fact]
    public async Task Build_Excludes_Blocked_And_Keep_Items()
    {
        var sessionId = await SeedSessionAndPlan(
            MakePlanItem("drv:oem1.inf", Recommendation.REMOVE_STAGE_1),
            MakePlanItem("drv:oem2.inf", Recommendation.BLOCKED),
            MakePlanItem("drv:oem3.inf", Recommendation.KEEP));

        var builder = new ActionQueueBuilder(_planRepo, _queueRepo);
        var queue = await builder.BuildAsync(sessionId);

        // 1 restore point + 1 remove item only
        Assert.Equal(2, queue.Actions.Count);
        Assert.Contains(queue.Actions, a => a.TargetId == "drv:oem1.inf");
        Assert.DoesNotContain(queue.Actions, a => a.TargetId == "drv:oem2.inf");
        Assert.DoesNotContain(queue.Actions, a => a.TargetId == "drv:oem3.inf");
    }

    [Fact]
    public async Task Build_Excludes_Unconfirmed_Review_Items()
    {
        var sessionId = await SeedSessionAndPlan(
            MakePlanItem("drv:oem1.inf", Recommendation.REVIEW));

        var builder = new ActionQueueBuilder(_planRepo, _queueRepo);
        var queue = await builder.BuildAsync(sessionId);

        // Only restore point — review not confirmed
        Assert.Single(queue.Actions);
        Assert.Equal(ActionType.CREATE_RESTORE_POINT, queue.Actions[0].ActionType);
    }

    [Fact]
    public async Task Build_Includes_Confirmed_Review_Items()
    {
        var sessionId = await SeedSessionAndPlan(
            MakePlanItem("drv:oem1.inf", Recommendation.REVIEW));

        var confirmed = new HashSet<string> { "drv:oem1.inf" };
        var builder = new ActionQueueBuilder(_planRepo, _queueRepo);
        var queue = await builder.BuildAsync(sessionId, confirmedReviewItemIds: confirmed);

        Assert.Equal(2, queue.Actions.Count);
        Assert.Contains(queue.Actions, a => a.TargetId == "drv:oem1.inf");
    }

    [Fact]
    public async Task Build_Sets_DryRun_Mode()
    {
        var sessionId = await SeedSessionAndPlan(
            MakePlanItem("drv:oem1.inf", Recommendation.REMOVE_STAGE_1));

        var builder = new ActionQueueBuilder(_planRepo, _queueRepo);
        var queue = await builder.BuildAsync(sessionId, mode: ExecutionMode.DRY_RUN);

        Assert.Equal(ExecutionMode.DRY_RUN, queue.Mode);
    }

    [Fact]
    public async Task Build_Persists_Queue_To_Database()
    {
        var sessionId = await SeedSessionAndPlan(
            MakePlanItem("drv:oem1.inf", Recommendation.REMOVE_STAGE_1));

        var builder = new ActionQueueBuilder(_planRepo, _queueRepo);
        var queue = await builder.BuildAsync(sessionId);

        var loaded = await _queueRepo.GetByIdAsync(queue.Id);
        Assert.NotNull(loaded);
        Assert.Equal(queue.Id, loaded.Id);
        Assert.Equal(queue.Actions.Count, loaded.Actions.Count);
    }

    [Fact]
    public void InferActionType_Maps_Drv_To_UninstallDriverPackage()
    {
        Assert.Equal(ActionType.UNINSTALL_DRIVER_PACKAGE, ActionQueueBuilder.InferActionType("drv:oem42.inf"));
    }

    [Fact]
    public void InferActionType_Maps_Pkg_To_UninstallDriverPackage()
    {
        Assert.Equal(ActionType.UNINSTALL_DRIVER_PACKAGE, ActionQueueBuilder.InferActionType("pkg:oem42.inf"));
    }

    [Fact]
    public void InferActionType_Maps_Svc_To_DisableService()
    {
        Assert.Equal(ActionType.DISABLE_SERVICE, ActionQueueBuilder.InferActionType("svc:SomeService"));
    }

    [Fact]
    public void InferActionType_Maps_App_To_UninstallProgram()
    {
        Assert.Equal(ActionType.UNINSTALL_PROGRAM, ActionQueueBuilder.InferActionType("app:SomeApp"));
    }

    [Fact]
    public void InferActionType_Returns_Null_For_Unknown_Prefix()
    {
        Assert.Null(ActionQueueBuilder.InferActionType("unknown:thing"));
    }

    private static PlanItem MakePlanItem(string itemId, Recommendation recommendation) => new()
    {
        ItemId = itemId,
        BaselineScore = 50,
        AiScoreDelta = 0,
        FinalScore = 50,
        Recommendation = recommendation,
        HardBlocks = [],
        EngineRationale = [],
    };
}

/// <summary>
/// Minimal Dapper-free extension for test seeding.
/// </summary>
file static class SqliteConnectionTestExtensions
{
    internal static Task ExecuteAsync(this Microsoft.Data.Sqlite.SqliteConnection conn, string sql, object param)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var prop in param.GetType().GetProperties())
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@{prop.Name}";
            p.Value = prop.GetValue(param) ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        return cmd.ExecuteNonQueryAsync();
    }
}
