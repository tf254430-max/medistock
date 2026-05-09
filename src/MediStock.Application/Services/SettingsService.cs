using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public SettingsService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Settings.AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty, ct);
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        return await _db.Settings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
    }

    public async Task SetAsync(IDictionary<string, string?> values, CancellationToken ct = default)
    {
        var existing = await _db.Settings.ToListAsync(ct);
        var byKey = existing.ToDictionary(s => s.Key);

        var before = existing.ToDictionary(s => s.Key, s => s.Value);
        var after = new Dictionary<string, string?>(before);

        foreach (var (key, value) in values)
        {
            if (byKey.TryGetValue(key, out var setting))
            {
                setting.Value = value;
            }
            else
            {
                _db.Settings.Add(new Setting { Key = key, Value = value });
            }
            after[key] = value;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Settings", "global", "Update", before, after, ct);
    }
}
