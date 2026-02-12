using System.Text.Json;
using Dapper;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Data.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly DmcaDbContext _db;

    public SessionRepository(DmcaDbContext db) => _db = db;

    public async Task<Session> CreateAsync(Session session)
    {
        const string sql = """
            INSERT INTO sessions (id, created_at, updated_at, status, app_version)
            VALUES (@Id, @CreatedAt, @UpdatedAt, @Status, @AppVersion)
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            Id = session.Id.ToString(),
            CreatedAt = session.CreatedAt.ToString("O"),
            UpdatedAt = session.UpdatedAt.ToString("O"),
            Status = session.Status.ToString(),
            AppVersion = session.AppVersion,
        });

        return session;
    }

    public async Task<Session?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM sessions WHERE id = @Id";

        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<SessionRow>(sql, new { Id = id.ToString() });
        return row?.ToSession();
    }

    public async Task<Session?> GetCurrentAsync()
    {
        const string sql = "SELECT * FROM sessions ORDER BY created_at DESC LIMIT 1";

        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<SessionRow>(sql);
        return row?.ToSession();
    }

    public async Task UpdateAsync(Session session)
    {
        const string sql = """
            UPDATE sessions
            SET updated_at = @UpdatedAt,
                status     = @Status
            WHERE id = @Id
            """;

        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(sql, new
        {
            Id = session.Id.ToString(),
            UpdatedAt = session.UpdatedAt.ToString("O"),
            Status = session.Status.ToString(),
        });

        if (affected == 0)
            throw new InvalidOperationException($"Session {session.Id} not found for update.");
    }

    /// <summary>
    /// Internal row type for Dapper mapping.
    /// </summary>
    private sealed class SessionRow
    {
        public string id { get; set; } = "";
        public string created_at { get; set; } = "";
        public string updated_at { get; set; } = "";
        public string status { get; set; } = "";
        public string app_version { get; set; } = "";

        public Session ToSession() => new()
        {
            Id = Guid.Parse(id),
            CreatedAt = DateTime.Parse(created_at).ToUniversalTime(),
            UpdatedAt = DateTime.Parse(updated_at).ToUniversalTime(),
            Status = Enum.Parse<SessionStatus>(status),
            AppVersion = app_version,
        };
    }
}
