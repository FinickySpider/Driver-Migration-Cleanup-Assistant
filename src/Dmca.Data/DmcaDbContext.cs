using Microsoft.Data.Sqlite;

namespace Dmca.Data;

/// <summary>
/// Manages the SQLite connection and ensures the schema is created.
/// Thread-safe: each call to <see cref="CreateConnection"/> returns a new, opened connection.
/// </summary>
public sealed class DmcaDbContext
{
    private readonly string _connectionString;

    public DmcaDbContext(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <summary>
    /// Convenience factory for a file-backed database.
    /// </summary>
    public static DmcaDbContext FromFile(string dbPath) =>
        new($"Data Source={dbPath}");

    /// <summary>
    /// Convenience factory for an in-memory database (useful for tests).
    /// Note: in-memory databases are destroyed when the last connection closes,
    /// so use a shared cache (<c>Mode=Memory;Cache=Shared</c>) or keep a connection open.
    /// </summary>
    public static DmcaDbContext InMemory(string name = "dmca") =>
        new($"Data Source={name};Mode=Memory;Cache=Shared");

    /// <summary>
    /// Creates and returns a new open <see cref="SqliteConnection"/>.
    /// The caller owns the connection and must dispose it.
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    /// <summary>
    /// Ensures the database schema is up-to-date.
    /// Safe to call multiple times (uses IF NOT EXISTS).
    /// </summary>
    public void EnsureSchema()
    {
        using var conn = CreateConnection();
        conn.Execute(Schema);
    }

    internal const string Schema = """
        PRAGMA journal_mode = WAL;
        PRAGMA foreign_keys = ON;

        CREATE TABLE IF NOT EXISTS sessions (
            id              TEXT    PRIMARY KEY,
            created_at      TEXT    NOT NULL,
            updated_at      TEXT    NOT NULL,
            status          TEXT    NOT NULL,
            app_version     TEXT    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS user_facts (
            session_id      TEXT    NOT NULL,
            key             TEXT    NOT NULL,
            value           TEXT    NOT NULL,
            source          TEXT    NOT NULL,
            created_at      TEXT    NOT NULL,
            FOREIGN KEY (session_id) REFERENCES sessions(id)
        );

        CREATE INDEX IF NOT EXISTS idx_user_facts_session
            ON user_facts(session_id);

        CREATE TABLE IF NOT EXISTS snapshots (
            id              TEXT    PRIMARY KEY,
            session_id      TEXT    NOT NULL,
            created_at      TEXT    NOT NULL,
            summary_json    TEXT    NOT NULL,
            FOREIGN KEY (session_id) REFERENCES sessions(id)
        );

        CREATE INDEX IF NOT EXISTS idx_snapshots_session
            ON snapshots(session_id);

        CREATE TABLE IF NOT EXISTS inventory_items (
            item_id                     TEXT    NOT NULL,
            snapshot_id                 TEXT    NOT NULL,
            item_type                   TEXT    NOT NULL,
            display_name                TEXT    NOT NULL,
            vendor                      TEXT,
            provider                    TEXT,
            version                     TEXT,
            driver_inf                  TEXT,
            driver_store_published_name TEXT,
            device_hardware_ids_json    TEXT,
            present                     INTEGER,
            running                     INTEGER,
            start_type                  INTEGER,
            signature_json              TEXT,
            paths_json                  TEXT,
            install_date                TEXT,
            last_loaded_date            TEXT,
            dependencies_json           TEXT,
            PRIMARY KEY (item_id, snapshot_id),
            FOREIGN KEY (snapshot_id) REFERENCES snapshots(id)
        );

        CREATE INDEX IF NOT EXISTS idx_inventory_items_snapshot
            ON inventory_items(snapshot_id);

        CREATE TABLE IF NOT EXISTS plans (
            id              TEXT    PRIMARY KEY,
            session_id      TEXT    NOT NULL,
            created_at      TEXT    NOT NULL,
            FOREIGN KEY (session_id) REFERENCES sessions(id)
        );

        CREATE INDEX IF NOT EXISTS idx_plans_session
            ON plans(session_id);

        CREATE TABLE IF NOT EXISTS plan_items (
            plan_id             TEXT    NOT NULL,
            item_id             TEXT    NOT NULL,
            baseline_score      INTEGER NOT NULL,
            ai_score_delta      INTEGER NOT NULL DEFAULT 0,
            final_score         INTEGER NOT NULL,
            recommendation      TEXT    NOT NULL,
            hard_blocks_json    TEXT    NOT NULL DEFAULT '[]',
            engine_rationale_json TEXT  NOT NULL DEFAULT '[]',
            ai_rationale_json   TEXT    NOT NULL DEFAULT '[]',
            notes_json          TEXT    NOT NULL DEFAULT '[]',
            blocked_reason      TEXT,
            PRIMARY KEY (plan_id, item_id),
            FOREIGN KEY (plan_id) REFERENCES plans(id)
        );

        CREATE TABLE IF NOT EXISTS proposals (
            id              TEXT    PRIMARY KEY,
            session_id      TEXT    NOT NULL,
            title           TEXT    NOT NULL,
            status          TEXT    NOT NULL DEFAULT 'PENDING',
            risk            TEXT    NOT NULL DEFAULT 'LOW',
            created_at      TEXT    NOT NULL,
            updated_at      TEXT    NOT NULL,
            changes_json    TEXT    NOT NULL DEFAULT '[]',
            evidence_json   TEXT    NOT NULL DEFAULT '[]',
            FOREIGN KEY (session_id) REFERENCES sessions(id)
        );

        CREATE INDEX IF NOT EXISTS idx_proposals_session
            ON proposals(session_id);
        """;
}

/// <summary>
/// Minimal extension so DmcaDbContext can execute DDL without pulling in Dapper at this level.
/// </summary>
internal static class SqliteConnectionExtensions
{
    internal static void Execute(this SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
