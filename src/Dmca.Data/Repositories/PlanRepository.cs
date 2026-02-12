using System.Text.Json;
using Dapper;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Data.Repositories;

public sealed class PlanRepository : IPlanRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly DmcaDbContext _db;

    public PlanRepository(DmcaDbContext db) => _db = db;

    public async Task CreateAsync(DecisionPlan plan)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        const string planSql = """
            INSERT INTO plans (id, session_id, created_at)
            VALUES (@Id, @SessionId, @CreatedAt)
            """;

        await conn.ExecuteAsync(planSql, new
        {
            Id = plan.Id.ToString(),
            SessionId = plan.SessionId.ToString(),
            CreatedAt = plan.CreatedAt.ToString("O"),
        }, tx);

        const string itemSql = """
            INSERT INTO plan_items (
                plan_id, item_id, baseline_score, ai_score_delta, final_score,
                recommendation, hard_blocks_json, engine_rationale_json,
                ai_rationale_json, notes_json, blocked_reason
            ) VALUES (
                @PlanId, @ItemId, @BaselineScore, @AiScoreDelta, @FinalScore,
                @Recommendation, @HardBlocksJson, @EngineRationaleJson,
                @AiRationaleJson, @NotesJson, @BlockedReason
            )
            """;

        foreach (var item in plan.Items)
        {
            await conn.ExecuteAsync(itemSql, new
            {
                PlanId = plan.Id.ToString(),
                ItemId = item.ItemId,
                BaselineScore = item.BaselineScore,
                AiScoreDelta = item.AiScoreDelta,
                FinalScore = item.FinalScore,
                Recommendation = item.Recommendation.ToString(),
                HardBlocksJson = JsonSerializer.Serialize(item.HardBlocks, JsonOpts),
                EngineRationaleJson = JsonSerializer.Serialize(item.EngineRationale, JsonOpts),
                AiRationaleJson = JsonSerializer.Serialize(item.AiRationale, JsonOpts),
                NotesJson = JsonSerializer.Serialize(item.Notes, JsonOpts),
                BlockedReason = item.BlockedReason,
            }, tx);
        }

        tx.Commit();
    }

    public async Task<DecisionPlan?> GetByIdAsync(Guid id)
    {
        const string planSql = "SELECT * FROM plans WHERE id = @Id";

        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<PlanRow>(planSql, new { Id = id.ToString() });
        if (row is null) return null;

        var items = await GetPlanItemsAsync(conn, id);
        return row.ToPlan(items);
    }

    public async Task<DecisionPlan?> GetCurrentBySessionIdAsync(Guid sessionId)
    {
        const string sql = """
            SELECT * FROM plans
            WHERE session_id = @SessionId
            ORDER BY created_at DESC
            LIMIT 1
            """;

        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<PlanRow>(sql,
            new { SessionId = sessionId.ToString() });
        if (row is null) return null;

        var planId = Guid.Parse(row.id);
        var items = await GetPlanItemsAsync(conn, planId);
        return row.ToPlan(items);
    }

    public async Task UpdatePlanItemAsync(Guid planId, PlanItem item)
    {
        const string sql = """
            UPDATE plan_items
            SET ai_score_delta = @AiScoreDelta,
                final_score = @FinalScore,
                recommendation = @Recommendation,
                ai_rationale_json = @AiRationaleJson,
                notes_json = @NotesJson,
                hard_blocks_json = @HardBlocksJson,
                blocked_reason = @BlockedReason
            WHERE plan_id = @PlanId AND item_id = @ItemId
            """;

        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            PlanId = planId.ToString(),
            ItemId = item.ItemId,
            AiScoreDelta = item.AiScoreDelta,
            FinalScore = item.FinalScore,
            Recommendation = item.Recommendation.ToString(),
            AiRationaleJson = JsonSerializer.Serialize(item.AiRationale, JsonOpts),
            NotesJson = JsonSerializer.Serialize(item.Notes, JsonOpts),
            HardBlocksJson = JsonSerializer.Serialize(item.HardBlocks, JsonOpts),
            BlockedReason = item.BlockedReason,
        });
    }

    private static async Task<IReadOnlyList<PlanItem>> GetPlanItemsAsync(
        Microsoft.Data.Sqlite.SqliteConnection conn, Guid planId)
    {
        const string sql = "SELECT * FROM plan_items WHERE plan_id = @PlanId";

        var rows = await conn.QueryAsync<PlanItemRow>(sql, new { PlanId = planId.ToString() });
        return rows.Select(r => r.ToPlanItem()).ToList().AsReadOnly();
    }

    // ── Row types ──

    private sealed class PlanRow
    {
        public string id { get; set; } = "";
        public string session_id { get; set; } = "";
        public string created_at { get; set; } = "";

        public DecisionPlan ToPlan(IReadOnlyList<PlanItem> items) => new()
        {
            Id = Guid.Parse(id),
            SessionId = Guid.Parse(session_id),
            CreatedAt = DateTime.Parse(created_at).ToUniversalTime(),
            Items = items,
        };
    }

    private sealed class PlanItemRow
    {
        public string plan_id { get; set; } = "";
        public string item_id { get; set; } = "";
        public int baseline_score { get; set; }
        public int ai_score_delta { get; set; }
        public int final_score { get; set; }
        public string recommendation { get; set; } = "";
        public string hard_blocks_json { get; set; } = "[]";
        public string engine_rationale_json { get; set; } = "[]";
        public string ai_rationale_json { get; set; } = "[]";
        public string notes_json { get; set; } = "[]";
        public string? blocked_reason { get; set; }

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public PlanItem ToPlanItem() => new()
        {
            ItemId = item_id,
            BaselineScore = baseline_score,
            AiScoreDelta = ai_score_delta,
            FinalScore = final_score,
            Recommendation = Enum.Parse<Recommendation>(recommendation),
            HardBlocks = JsonSerializer.Deserialize<List<HardBlock>>(hard_blocks_json, JsonOpts)
                ?? [],
            EngineRationale = JsonSerializer.Deserialize<List<string>>(engine_rationale_json, JsonOpts)
                ?? [],
            AiRationale = JsonSerializer.Deserialize<List<string>>(ai_rationale_json, JsonOpts)
                ?? [],
            Notes = JsonSerializer.Deserialize<List<string>>(notes_json, JsonOpts)
                ?? [],
            BlockedReason = blocked_reason,
        };
    }
}
