using MediStock.Application.Interfaces;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MediStock.Application.Services;

public class SqliteReceiptNumberGenerator : IReceiptNumberGenerator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _lock = new();
    private int _lastSeq;
    private int _lastYear;
    private bool _initialised;

    public SqliteReceiptNumberGenerator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string> NextAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;

        if (!_initialised || _lastYear != year)
        {
            await InitialiseForYearAsync(year, ct);
        }

        int next;
        lock (_lock)
        {
            _lastSeq += 1;
            next = _lastSeq;
        }
        return $"R-{year}-{next:D6}";
    }

    private async Task InitialiseForYearAsync(int year, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var prefix = $"R-{year}-";

        var existing = await db.Sales
            .Where(s => s.ReceiptNumber.StartsWith(prefix))
            .Select(s => s.ReceiptNumber)
            .ToListAsync(ct);

        var maxSeq = 0;
        foreach (var rn in existing)
        {
            if (rn.Length > prefix.Length
                && int.TryParse(rn.AsSpan(prefix.Length), out var s))
            {
                if (s > maxSeq) maxSeq = s;
            }
        }

        lock (_lock)
        {
            if (!_initialised || _lastYear != year)
            {
                _lastYear = year;
                _lastSeq = maxSeq;
                _initialised = true;
            }
        }
    }
}
