using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class InventoryAlertService : IInventoryAlertService
{
    private readonly AppDbContext _db;

    public InventoryAlertService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<LowStockDrugDto>> GetLowStockAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Drugs.AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => new LowStockDrugDto
            {
                DrugId = d.Id,
                DrugName = d.Name,
                CategoryName = d.Category.Name,
                ReorderLevel = d.ReorderLevel,
                CurrentStock = d.Batches
                    .Where(b => b.IsActive && b.ExpiryDate > now)
                    .Sum(b => b.QuantityRemaining)
            })
            .Where(x => x.CurrentStock <= x.ReorderLevel)
            .OrderBy(x => x.CurrentStock)
            .ThenBy(x => x.DrugName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ExpiringBatchDto>> GetExpiringSoonAsync(int withinDays = 30, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(withinDays);

        var raw = await _db.Batches.AsNoTracking()
            .Where(b => b.IsActive && b.QuantityRemaining > 0
                     && b.ExpiryDate > now && b.ExpiryDate <= cutoff)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new
            {
                b.Id, b.DrugId, DrugName = b.Drug.Name,
                b.BatchNumber, b.QuantityRemaining, b.ExpiryDate
            })
            .ToListAsync(ct);

        return raw.Select(b => new ExpiringBatchDto
        {
            BatchId = b.Id,
            DrugId = b.DrugId,
            DrugName = b.DrugName,
            BatchNumber = b.BatchNumber,
            QuantityRemaining = b.QuantityRemaining,
            ExpiryDate = b.ExpiryDate,
            DaysUntilExpiry = (int)(b.ExpiryDate.Date - now.Date).TotalDays
        }).ToList();
    }

    public async Task<InventoryAlertSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(30);

        var lowStockCount = await _db.Drugs.AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => new
            {
                d.ReorderLevel,
                Stock = d.Batches.Where(b => b.IsActive && b.ExpiryDate > now).Sum(b => b.QuantityRemaining)
            })
            .Where(x => x.Stock <= x.ReorderLevel)
            .CountAsync(ct);

        var expiringCount = await _db.Batches.AsNoTracking()
            .CountAsync(b => b.IsActive && b.QuantityRemaining > 0
                          && b.ExpiryDate > now && b.ExpiryDate <= cutoff, ct);

        return new InventoryAlertSummary
        {
            LowStockCount = lowStockCount,
            ExpiringSoonCount = expiringCount
        };
    }
}
