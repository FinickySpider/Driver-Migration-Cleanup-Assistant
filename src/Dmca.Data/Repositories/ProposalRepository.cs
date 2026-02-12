using System.Text.Json;
using Dapper;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Data.Repositories;

public sealed class ProposalRepository : IProposalRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly DmcaDbContext _db;

    public ProposalRepository(DmcaDbContext db) => _db = db;

    public async Task CreateAsync(Proposal proposal)
    {
        const string sql = """
            INSERT INTO proposals (id, session_id, title, status, risk, created_at, updated_at, changes_json, evidence_json)
            VALUES (@Id, @SessionId, @Title, @Status, @Risk, @CreatedAt, @UpdatedAt, @ChangesJson, @EvidenceJson)
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            Id = proposal.Id.ToString(),
            SessionId = proposal.SessionId.ToString(),
            Title = proposal.Title,
            Status = proposal.Status.ToString(),
            Risk = proposal.Risk.ToString(),
            CreatedAt = proposal.CreatedAt.ToString("O"),
            UpdatedAt = proposal.UpdatedAt.ToString("O"),
            ChangesJson = JsonSerializer.Serialize(proposal.Changes, JsonOpts),
            EvidenceJson = JsonSerializer.Serialize(proposal.Evidence, JsonOpts),
        });
    }

    public async Task<Proposal?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM proposals WHERE id = @Id";

        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<ProposalRow>(sql, new { Id = id.ToString() });
        return row?.ToProposal();
    }

    public async Task<IReadOnlyList<Proposal>> GetBySessionIdAsync(Guid sessionId)
    {
        const string sql = """
            SELECT * FROM proposals
            WHERE session_id = @SessionId
            ORDER BY created_at
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<ProposalRow>(sql, new { SessionId = sessionId.ToString() });
        return rows.Select(r => r.ToProposal()).ToList().AsReadOnly();
    }

    public async Task UpdateStatusAsync(Guid id, ProposalStatus status, DateTime updatedAt)
    {
        const string sql = """
            UPDATE proposals
            SET status = @Status, updated_at = @UpdatedAt
            WHERE id = @Id
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            Status = status.ToString(),
            UpdatedAt = updatedAt.ToString("O"),
        });
    }

    private sealed class ProposalRow
    {
        public string id { get; set; } = "";
        public string session_id { get; set; } = "";
        public string title { get; set; } = "";
        public string status { get; set; } = "";
        public string risk { get; set; } = "";
        public string created_at { get; set; } = "";
        public string updated_at { get; set; } = "";
        public string changes_json { get; set; } = "[]";
        public string evidence_json { get; set; } = "[]";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public Proposal ToProposal() => new()
        {
            Id = Guid.Parse(id),
            SessionId = Guid.Parse(session_id),
            Title = title,
            Status = Enum.Parse<ProposalStatus>(status),
            Risk = Enum.Parse<EstimatedRisk>(risk),
            CreatedAt = DateTime.Parse(created_at).ToUniversalTime(),
            UpdatedAt = DateTime.Parse(updated_at).ToUniversalTime(),
            Changes = JsonSerializer.Deserialize<List<ProposalChange>>(changes_json, JsonOpts)
                ?? [],
            Evidence = JsonSerializer.Deserialize<List<Evidence>>(evidence_json, JsonOpts)
                ?? [],
        };
    }
}
