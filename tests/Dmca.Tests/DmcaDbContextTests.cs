using Dmca.Data;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="DmcaDbContext"/> schema initialization.
/// </summary>
public sealed class DmcaDbContextTests : IDisposable
{
    private readonly DmcaDbContext _db;
    private readonly Microsoft.Data.Sqlite.SqliteConnection _keepAlive;

    public DmcaDbContextTests()
    {
        _db = DmcaDbContext.InMemory($"ctx_test_{Guid.NewGuid():N}");
        _keepAlive = _db.CreateConnection();
    }

    public void Dispose() => _keepAlive.Dispose();

    [Fact]
    public void EnsureSchema_CreatesAllTables()
    {
        _db.EnsureSchema();

        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";

        var tables = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            tables.Add(reader.GetString(0));

        Assert.Contains("sessions", tables);
        Assert.Contains("user_facts", tables);
        Assert.Contains("snapshots", tables);
        Assert.Contains("inventory_items", tables);
    }

    [Fact]
    public void EnsureSchema_IsIdempotent()
    {
        _db.EnsureSchema();
        var ex = Record.Exception(() => _db.EnsureSchema());
        Assert.Null(ex);
    }

    [Fact]
    public void CreateConnection_ReturnsOpenConnection()
    {
        using var conn = _db.CreateConnection();
        Assert.Equal(System.Data.ConnectionState.Open, conn.State);
    }
}
