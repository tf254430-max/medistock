using MediStock.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediStock.IntegrationTests;

/// <summary>
/// Spins up the real Web pipeline against an isolated SQLite database file
/// living in the system temp folder. The database is removed when the factory
/// is disposed so each test class starts from a clean slate.
/// </summary>
public class MediStockWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public MediStockWebFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"medistock-test-{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace any registered DbContext with one bound to the test SQLite file.
            var existing = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList();
            foreach (var d in existing) services.Remove(d);
            services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
            var shm = _dbPath + "-shm";
            var wal = _dbPath + "-wal";
            if (File.Exists(shm)) File.Delete(shm);
            if (File.Exists(wal)) File.Delete(wal);
        }
        catch { /* best effort */ }
    }
}
