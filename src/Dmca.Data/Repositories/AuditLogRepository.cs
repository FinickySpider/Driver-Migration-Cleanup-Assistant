using Dapper;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Data.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly DmcaDbContext _db;

    public AuditLogRepository(DmcaDbContext db) => _db = db;

    public async Task CreateAsync(AuditLogEntry entry)
    {
        using var conn = _db.CreateConnection();
        const string sql = """
            INSERT INTO audit_log (
                id, session_id, action_id, action_type, target_id,
                status, timestamp, output, error_message
            ) VALUES (
                @Id, @SessionId, @ActionId, @ActionType, @TargetId,
                @Status, @Timestamp, @Output, @ErrorMessage
            )
            """;

        await conn.ExecuteAsync(sql, new
        {
            Id = entry.Id.ToString(),
            SessionId = entry.SessionId.ToString(),
            ActionId = entry.ActionId.ToString(),
            ActionType = entry.ActionType.ToString(),
            TargetId = entry.TargetId,
            Status = entry.Status.ToString(),
            Timestamp = entry.Timestamp.ToString("O"),
            Output = entry.Output,
            ErrorMessage = entry.ErrorMessage,
        });
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetBySessionIdAsync(Guid sessionId)
    {
        using var conn = _db.CreateConnection();
        const string sql = """
            SELECT id, session_id, action_id, action_type, target_id,
                   status, timestamp, output, error_message
            FROM audit_log
            WHERE session_id = @SessionId
            ORDER BY timestamp
            """;

        var rows = await conn.QueryAsync(sql, new { SessionId = sessionId.ToString() });
        return MapRows(rows);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetByActionIdAsync(Guid actionId)
    {
        using var conn = _db.CreateConnection();
        const string sql = """
            SELECT id, session_id, action_id, action_type, target_id,
                   status, timestamp, output, error_message
            FROM audit_log
            WHERE action_id = @ActionId
            ORDER BY timestamp
            """;

        var rows = await conn.QueryAsync(sql, new { ActionId = actionId.ToString() });
        return MapRows(rows);
    }

    private static IReadOnlyList<AuditLogEntry> MapRows(IEnumerable<dynamic> rows)
    {
        var results = new List<AuditLogEntry>();
        foreach (var r in rows)
        {
            results.Add(new AuditLogEntry
            {
                Id = Guid.Parse((string)r.id),
                SessionId = Guid.Parse((string)r.session_id),
                ActionId = Guid.Parse((string)r.action_id),
                ActionType = Enum.Parse<ActionType>((string)r.action_type),
                TargetId = (string)r.target_id,
                Status = Enum.Parse<ActionStatus>((string)r.status),
                Timestamp = DateTime.Parse((string)r.timestamp),
                Output = (string?)r.output,
                ErrorMessage = (string?)r.error_message,
            });
        }
        return results;
    }
}
