using System.Text.Json;
using Dapper;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Data.Repositories;

public sealed class SnapshotRepository : ISnapshotRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly DmcaDbContext _db;

    public SnapshotRepository(DmcaDbContext db) => _db = db;

    public async Task CreateAsync(InventorySnapshot snapshot)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();

        // Insert snapshot header
        const string snapshotSql = """
            INSERT INTO snapshots (id, session_id, created_at, summary_json)
            VALUES (@Id, @SessionId, @CreatedAt, @SummaryJson)
            """;

        await conn.ExecuteAsync(snapshotSql, new
        {
            Id = snapshot.Id.ToString(),
            SessionId = snapshot.SessionId.ToString(),
            CreatedAt = snapshot.CreatedAt.ToString("O"),
            SummaryJson = JsonSerializer.Serialize(snapshot.Summary, JsonOpts),
        }, tx);

        // Insert inventory items
        const string itemSql = """
            INSERT INTO inventory_items (
                item_id, snapshot_id, item_type, display_name,
                vendor, provider, version, driver_inf,
                driver_store_published_name, device_hardware_ids_json,
                present, running, start_type, signature_json,
                paths_json, install_date, last_loaded_date, dependencies_json
            ) VALUES (
                @ItemId, @SnapshotId, @ItemType, @DisplayName,
                @Vendor, @Provider, @Version, @DriverInf,
                @DriverStorePublishedName, @DeviceHardwareIdsJson,
                @Present, @Running, @StartType, @SignatureJson,
                @PathsJson, @InstallDate, @LastLoadedDate, @DependenciesJson
            )
            """;

        foreach (var item in snapshot.Items)
        {
            await conn.ExecuteAsync(itemSql, new
            {
                ItemId = item.ItemId,
                SnapshotId = snapshot.Id.ToString(),
                ItemType = item.ItemType.ToString(),
                DisplayName = item.DisplayName,
                Vendor = item.Vendor,
                Provider = item.Provider,
                Version = item.Version,
                DriverInf = item.DriverInf,
                DriverStorePublishedName = item.DriverStorePublishedName,
                DeviceHardwareIdsJson = item.DeviceHardwareIds is not null
                    ? JsonSerializer.Serialize(item.DeviceHardwareIds, JsonOpts) : null,
                Present = item.Present.HasValue ? (item.Present.Value ? 1 : 0) : (int?)null,
                Running = item.Running.HasValue ? (item.Running.Value ? 1 : 0) : (int?)null,
                StartType = item.StartType,
                SignatureJson = item.Signature is not null
                    ? JsonSerializer.Serialize(item.Signature, JsonOpts) : null,
                PathsJson = item.Paths is not null
                    ? JsonSerializer.Serialize(item.Paths, JsonOpts) : null,
                InstallDate = item.InstallDate?.ToString("O"),
                LastLoadedDate = item.LastLoadedDate?.ToString("O"),
                DependenciesJson = item.Dependencies is not null
                    ? JsonSerializer.Serialize(item.Dependencies, JsonOpts) : null,
            }, tx);
        }

        tx.Commit();
    }

    public async Task<InventorySnapshot?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM snapshots WHERE id = @Id";

        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<SnapshotRow>(sql, new { Id = id.ToString() });
        if (row is null) return null;

        var items = await GetItemsBySnapshotIdAsync(id);
        return row.ToSnapshot(items);
    }

    public async Task<InventorySnapshot?> GetLatestBySessionIdAsync(Guid sessionId)
    {
        const string sql = """
            SELECT * FROM snapshots
            WHERE session_id = @SessionId
            ORDER BY created_at DESC
            LIMIT 1
            """;

        using var conn = _db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<SnapshotRow>(sql,
            new { SessionId = sessionId.ToString() });
        if (row is null) return null;

        var snapshotId = Guid.Parse(row.id);
        var items = await GetItemsBySnapshotIdAsync(snapshotId);
        return row.ToSnapshot(items);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetItemsBySnapshotIdAsync(Guid snapshotId)
    {
        const string sql = "SELECT * FROM inventory_items WHERE snapshot_id = @SnapshotId";

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<InventoryItemRow>(sql,
            new { SnapshotId = snapshotId.ToString() });

        return rows.Select(r => r.ToInventoryItem()).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<InventorySnapshot>> GetAllBySessionIdAsync(Guid sessionId)
    {
        const string sql = """
            SELECT * FROM snapshots
            WHERE session_id = @SessionId
            ORDER BY created_at ASC
            """;

        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<SnapshotRow>(sql,
            new { SessionId = sessionId.ToString() });

        var snapshots = new List<InventorySnapshot>();
        foreach (var row in rows)
        {
            var snapshotId = Guid.Parse(row.id);
            var items = await GetItemsBySnapshotIdAsync(snapshotId);
            snapshots.Add(row.ToSnapshot(items));
        }

        return snapshots.AsReadOnly();
    }

    // ── Row types for Dapper mapping ──

    private sealed class SnapshotRow
    {
        public string id { get; set; } = "";
        public string session_id { get; set; } = "";
        public string created_at { get; set; } = "";
        public string summary_json { get; set; } = "";

        public InventorySnapshot ToSnapshot(IReadOnlyList<InventoryItem> items) => new()
        {
            Id = Guid.Parse(id),
            SessionId = Guid.Parse(session_id),
            CreatedAt = DateTime.Parse(created_at).ToUniversalTime(),
            Summary = JsonSerializer.Deserialize<SnapshotSummary>(summary_json, JsonOpts)
                ?? new SnapshotSummary(),
            Items = items,
        };

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    private sealed class InventoryItemRow
    {
        public string item_id { get; set; } = "";
        public string snapshot_id { get; set; } = "";
        public string item_type { get; set; } = "";
        public string display_name { get; set; } = "";
        public string? vendor { get; set; }
        public string? provider { get; set; }
        public string? version { get; set; }
        public string? driver_inf { get; set; }
        public string? driver_store_published_name { get; set; }
        public string? device_hardware_ids_json { get; set; }
        public int? present { get; set; }
        public int? running { get; set; }
        public int? start_type { get; set; }
        public string? signature_json { get; set; }
        public string? paths_json { get; set; }
        public string? install_date { get; set; }
        public string? last_loaded_date { get; set; }
        public string? dependencies_json { get; set; }

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public InventoryItem ToInventoryItem() => new()
        {
            ItemId = item_id,
            ItemType = Enum.Parse<InventoryItemType>(item_type),
            DisplayName = display_name,
            Vendor = vendor,
            Provider = provider,
            Version = version,
            DriverInf = driver_inf,
            DriverStorePublishedName = driver_store_published_name,
            DeviceHardwareIds = device_hardware_ids_json is not null
                ? JsonSerializer.Deserialize<List<string>>(device_hardware_ids_json, JsonOpts) : null,
            Present = present.HasValue ? present.Value != 0 : null,
            Running = running.HasValue ? running.Value != 0 : null,
            StartType = start_type,
            Signature = signature_json is not null
                ? JsonSerializer.Deserialize<SignatureInfo>(signature_json, JsonOpts) : null,
            Paths = paths_json is not null
                ? JsonSerializer.Deserialize<List<string>>(paths_json, JsonOpts) : null,
            InstallDate = install_date is not null ? DateTime.Parse(install_date).ToUniversalTime() : null,
            LastLoadedDate = last_loaded_date is not null ? DateTime.Parse(last_loaded_date).ToUniversalTime() : null,
            Dependencies = dependencies_json is not null
                ? JsonSerializer.Deserialize<List<string>>(dependencies_json, JsonOpts) : null,
        };
    }
}
