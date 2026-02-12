using Dapper;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Data.Repositories;

public sealed class UserFactRepository : IUserFactRepository
{
    private readonly DmcaDbContext _db;

    public UserFactRepository(DmcaDbContext db) => _db = db;

    public async Task AddAsync(UserFact fact)
    {
        const string sql = """
            INSERT INTO user_facts (session_id, key, value, source, created_at)
            VALUES (@SessionId, @Key, @Value, @Source, @CreatedAt)
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            SessionId = fact.SessionId.ToString(),
            Key = fact.Key,
            Value = fact.Value,
            Source = fact.Source.ToString(),
            CreatedAt = fact.CreatedAt.ToString("O"),
        });
    }

    public async Task AddRangeAsync(IEnumerable<UserFact> facts)
    {
        const string sql = """
            INSERT INTO user_facts (session_id, key, value, source, created_at)
            VALUES (@SessionId, @Key, @Value, @Source, @CreatedAt)
            """;

        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        foreach (var fact in facts)
        {
            await conn.ExecuteAsync(sql, new
            {
                SessionId = fact.SessionId.ToString(),
                Key = fact.Key,
                Value = fact.Value,
                Source = fact.Source.ToString(),
                CreatedAt = fact.CreatedAt.ToString("O"),
            }, tx);
        }

        tx.Commit();
    }

    public async Task<IReadOnlyList<UserFact>> GetBySessionIdAsync(Guid sessionId)
    {
        const string sql = """
            SELECT session_id, key, value, source, created_at
            FROM user_facts
            WHERE session_id = @SessionId
            ORDER BY created_at
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<UserFactRow>(sql, new { SessionId = sessionId.ToString() });

        return rows.Select(r => r.ToUserFact()).ToList().AsReadOnly();
    }

    private sealed class UserFactRow
    {
        public string session_id { get; set; } = "";
        public string key { get; set; } = "";
        public string value { get; set; } = "";
        public string source { get; set; } = "";
        public string created_at { get; set; } = "";

        public UserFact ToUserFact() => new()
        {
            SessionId = Guid.Parse(session_id),
            Key = key,
            Value = value,
            Source = Enum.Parse<FactSource>(source),
            CreatedAt = DateTime.Parse(created_at).ToUniversalTime(),
        };
    }
}
