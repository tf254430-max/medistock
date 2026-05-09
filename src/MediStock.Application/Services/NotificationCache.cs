using System.Collections.Concurrent;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MediStock.Application.Services;

public class NotificationCache : INotificationCache
{
    // ConcurrentDictionary used as the in-memory backing store per FR-NTF-04.
    // Key is the snapshot section name; value is the latest list/object.
    private readonly ConcurrentDictionary<string, object> _store = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public NotificationCache(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public NotificationSnapshot Current
    {
        get
        {
            var lastRefresh = _store.TryGetValue("lastRefreshedAt", out var t) ? (DateTime)t : DateTime.MinValue;
            var low = _store.TryGetValue("lowStock", out var l) ? (IReadOnlyList<LowStockDrugDto>)l : Array.Empty<LowStockDrugDto>();
            var exp = _store.TryGetValue("expiring", out var e) ? (IReadOnlyList<ExpiringBatchDto>)e : Array.Empty<ExpiringBatchDto>();
            return new NotificationSnapshot
            {
                LastRefreshedAt = lastRefresh,
                LowStock = low,
                ExpiringSoon = exp
            };
        }
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var alerts = scope.ServiceProvider.GetRequiredService<IInventoryAlertService>();
        var lowStock = await alerts.GetLowStockAsync(ct);
        var expiring = await alerts.GetExpiringSoonAsync(30, ct);

        _store["lowStock"] = lowStock;
        _store["expiring"] = expiring;
        _store["lastRefreshedAt"] = DateTime.UtcNow;

        // SMTP-ready code path — intentionally commented out per FR-NTF-04.
        // Enabling requires SMTP credentials in configuration; emails would otherwise
        // fail at marking time on a laptop without internet/SMTP setup.
        //
        // foreach (var d in lowStock)
        //     await _email.SendAsync(adminEmails, $"Low stock: {d.DrugName}",
        //         $"{d.DrugName} is at {d.CurrentStock} (reorder {d.ReorderLevel}).", ct);
        // foreach (var b in expiring)
        //     await _email.SendAsync(adminEmails, $"Batch expiring soon: {b.DrugName}",
        //         $"Batch {b.BatchNumber} expires {b.ExpiryDate:yyyy-MM-dd}.", ct);
    }
}
