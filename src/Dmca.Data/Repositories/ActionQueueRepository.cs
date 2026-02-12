using Dapper;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Data.Repositories;

public sealed class ActionQueueRepository : IActionQueueRepository
{
    private readonly DmcaDbContext _db;

    public ActionQueueRepository(DmcaDbContext db) => _db = db;

    public async Task CreateAsync(ActionQueue queue)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        const string queueSql = """
            INSERT INTO action_queues (id, session_id, created_at, mode, overall_status)
            VALUES (@Id, @SessionId, @CreatedAt, @Mode, @OverallStatus)
            """;

        await conn.ExecuteAsync(queueSql, new
        {
            Id = queue.Id.ToString(),
            SessionId = queue.SessionId.ToString(),
            CreatedAt = queue.CreatedAt.ToString("O"),
            Mode = queue.Mode.ToString(),
            OverallStatus = queue.OverallStatus.ToString(),
        }, tx);

        const string actionSql = """
            INSERT INTO actions (
                id, queue_id, order_num, action_type, target_id,
                display_name, status, command, output, error_message,
                started_at, completed_at
            ) VALUES (
                @Id, @QueueId, @OrderNum, @ActionType, @TargetId,
                @DisplayName, @Status, @Command, @Output, @ErrorMessage,
                @StartedAt, @CompletedAt
            )
            """;

        foreach (var action in queue.Actions)
        {
            await conn.ExecuteAsync(actionSql, new
            {
                Id = action.Id.ToString(),
                QueueId = queue.Id.ToString(),
                OrderNum = action.Order,
                ActionType = action.ActionType.ToString(),
                TargetId = action.TargetId,
                DisplayName = action.DisplayName,
                Status = action.Status.ToString(),
                Command = action.Command,
                Output = action.Output,
                ErrorMessage = action.ErrorMessage,
                StartedAt = action.StartedAt?.ToString("O"),
                CompletedAt = action.CompletedAt?.ToString("O"),
            }, tx);
        }

        tx.Commit();
    }

    public async Task<ActionQueue?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        return await LoadQueueAsync(conn, "WHERE aq.id = @Id", new { Id = id.ToString() });
    }

    public async Task<ActionQueue?> GetBySessionIdAsync(Guid sessionId)
    {
        using var conn = _db.CreateConnection();
        return await LoadQueueAsync(conn, "WHERE aq.session_id = @SessionId ORDER BY aq.created_at DESC LIMIT 1",
            new { SessionId = sessionId.ToString() });
    }

    public async Task UpdateActionAsync(ExecutionAction action)
    {
        using var conn = _db.CreateConnection();
        const string sql = """
            UPDATE actions SET
                status = @Status,
                command = @Command,
                output = @Output,
                error_message = @ErrorMessage,
                started_at = @StartedAt,
                completed_at = @CompletedAt
            WHERE id = @Id
            """;

        await conn.ExecuteAsync(sql, new
        {
            Id = action.Id.ToString(),
            Status = action.Status.ToString(),
            Command = action.Command,
            Output = action.Output,
            ErrorMessage = action.ErrorMessage,
            StartedAt = action.StartedAt?.ToString("O"),
            CompletedAt = action.CompletedAt?.ToString("O"),
        });
    }

    public async Task UpdateQueueStatusAsync(Guid queueId, ActionStatus status)
    {
        using var conn = _db.CreateConnection();
        const string sql = "UPDATE action_queues SET overall_status = @Status WHERE id = @Id";
        await conn.ExecuteAsync(sql, new { Id = queueId.ToString(), Status = status.ToString() });
    }

    private static async Task<ActionQueue?> LoadQueueAsync(
        Microsoft.Data.Sqlite.SqliteConnection conn,
        string whereClause,
        object param)
    {
        var queueSql = $"""
            SELECT id, session_id, created_at, mode, overall_status
            FROM action_queues aq
            {whereClause}
            """;

        var row = await conn.QueryFirstOrDefaultAsync(queueSql, param);
        if (row is null) return null;

        string queueId = (string)row.id;
        var queue = new ActionQueue
        {
            Id = Guid.Parse(queueId),
            SessionId = Guid.Parse((string)row.session_id),
            CreatedAt = DateTime.Parse((string)row.created_at),
            Mode = Enum.Parse<ExecutionMode>((string)row.mode),
            OverallStatus = Enum.Parse<ActionStatus>((string)row.overall_status),
        };

        const string actionsSql = """
            SELECT id, order_num, action_type, target_id, display_name,
                   status, command, output, error_message, started_at, completed_at
            FROM actions
            WHERE queue_id = @QueueId
            ORDER BY order_num
            """;

        var actionRows = await conn.QueryAsync(actionsSql, new { QueueId = queueId });
        foreach (var a in actionRows)
        {
            queue.Actions.Add(new ExecutionAction
            {
                Id = Guid.Parse((string)a.id),
                Order = (int)(long)a.order_num,
                ActionType = Enum.Parse<ActionType>((string)a.action_type),
                TargetId = (string)a.target_id,
                DisplayName = (string)a.display_name,
                Status = Enum.Parse<ActionStatus>((string)a.status),
                Command = (string?)a.command,
                Output = (string?)a.output,
                ErrorMessage = (string?)a.error_message,
                StartedAt = a.started_at is string sa ? DateTime.Parse(sa) : null,
                CompletedAt = a.completed_at is string ca ? DateTime.Parse(ca) : null,
            });
        }

        return queue;
    }
}
