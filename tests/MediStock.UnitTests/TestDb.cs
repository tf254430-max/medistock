using MediStock.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MediStock.UnitTests;

/// <summary>
/// Test helper backed by a SQLite ":memory:" database.
/// Mirrors production EF behaviour (HasConversion, EF.Functions.Like, transactions)
/// and is recreated fresh for every test, keeping tests isolated.
/// </summary>
public static class TestDb
{
    /// <summary>
    /// Creates an isolated in-memory SQLite-backed AppDbContext with the schema applied.
    /// The connection lives as long as the returned context (disposed together).
    /// </summary>
    public static AppDbContext CreateInMemory()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var ctx = new AppDbContext(opts);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
