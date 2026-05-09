using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class BatchService : IBatchService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _httpContext;

    public BatchService(AppDbContext db, IAuditService audit, IHttpContextAccessor httpContext)
    {
        _db = db;
        _audit = audit;
        _httpContext = httpContext;
    }

    public async Task<PagedResult<BatchListItemDto>> ListAsync(PageRequest request, int? drugId, CancellationToken ct = default)
    {
        IQueryable<Batch> q = _db.Batches.AsNoTracking();

        if (drugId is int id) q = q.Where(b => b.DrugId == id);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(b => EF.Functions.Like(b.BatchNumber, $"%{s}%")
                          || EF.Functions.Like(b.Drug.Name, $"%{s}%")
                          || EF.Functions.Like(b.Supplier.Name, $"%{s}%"));
        }

        q = (request.Sort?.ToLowerInvariant(), request.Descending) switch
        {
            ("expiry", true) => q.OrderByDescending(b => b.ExpiryDate),
            ("expiry", false) => q.OrderBy(b => b.ExpiryDate),
            ("drug", true) => q.OrderByDescending(b => b.Drug.Name),
            ("drug", false) => q.OrderBy(b => b.Drug.Name),
            ("received", true) => q.OrderByDescending(b => b.ReceivedAt),
            ("received", false) => q.OrderBy(b => b.ReceivedAt),
            _ => q.OrderBy(b => b.ExpiryDate)
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BatchListItemDto
            {
                Id = b.Id,
                DrugId = b.DrugId,
                DrugName = b.Drug.Name,
                BatchNumber = b.BatchNumber,
                SupplierId = b.SupplierId,
                SupplierName = b.Supplier.Name,
                QuantityIn = b.QuantityIn,
                QuantityRemaining = b.QuantityRemaining,
                CostPrice = b.CostPrice,
                SellPrice = b.SellPrice,
                ExpiryDate = b.ExpiryDate,
                ReceivedAt = b.ReceivedAt,
                IsActive = b.IsActive
            })
            .ToListAsync(ct);

        return new PagedResult<BatchListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<BatchListItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Batches.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BatchListItemDto
            {
                Id = b.Id,
                DrugId = b.DrugId,
                DrugName = b.Drug.Name,
                BatchNumber = b.BatchNumber,
                SupplierId = b.SupplierId,
                SupplierName = b.Supplier.Name,
                QuantityIn = b.QuantityIn,
                QuantityRemaining = b.QuantityRemaining,
                CostPrice = b.CostPrice,
                SellPrice = b.SellPrice,
                ExpiryDate = b.ExpiryDate,
                ReceivedAt = b.ReceivedAt,
                IsActive = b.IsActive
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<BatchListItemDto>> ListForDrugAsync(int drugId, CancellationToken ct = default)
    {
        return await _db.Batches.AsNoTracking()
            .Where(b => b.DrugId == drugId)
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new BatchListItemDto
            {
                Id = b.Id,
                DrugId = b.DrugId,
                DrugName = b.Drug.Name,
                BatchNumber = b.BatchNumber,
                SupplierId = b.SupplierId,
                SupplierName = b.Supplier.Name,
                QuantityIn = b.QuantityIn,
                QuantityRemaining = b.QuantityRemaining,
                CostPrice = b.CostPrice,
                SellPrice = b.SellPrice,
                ExpiryDate = b.ExpiryDate,
                ReceivedAt = b.ReceivedAt,
                IsActive = b.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<int> CreateAsync(BatchCreateDto dto, CancellationToken ct = default)
    {
        var userId = _httpContext.HttpContext?.Items[AuditService.UserIdItemKey] as string
            ?? throw new InvalidOperationException("No authenticated user on request.");

        var batch = new Batch
        {
            DrugId = dto.DrugId,
            BatchNumber = dto.BatchNumber.Trim(),
            SupplierId = dto.SupplierId,
            QuantityIn = dto.Quantity,
            QuantityRemaining = dto.Quantity,
            CostPrice = dto.CostPrice,
            SellPrice = dto.SellPrice,
            ExpiryDate = DateTime.SpecifyKind(dto.ExpiryDate, DateTimeKind.Utc),
            ReceivedAt = DateTime.UtcNow,
            IsActive = true
        };
        _db.Batches.Add(batch);
        await _db.SaveChangesAsync(ct);

        _db.StockMovements.Add(new StockMovement
        {
            BatchId = batch.Id,
            MovementType = MovementType.In,
            Quantity = dto.Quantity,
            Reference = $"BATCH-IN/{batch.BatchNumber}",
            PerformedById = userId,
            PerformedAt = DateTime.UtcNow,
            Notes = "Initial stock-in"
        });
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Batch", batch.Id.ToString(), "Create", null, Snapshot(batch), ct);
        return batch.Id;
    }

    public async Task<int> MarkExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _db.Batches
            .Where(b => b.IsActive && b.ExpiryDate <= now && b.QuantityRemaining > 0)
            .ToListAsync(ct);

        if (expired.Count == 0) return 0;

        var systemUserId = _httpContext.HttpContext?.Items[AuditService.UserIdItemKey] as string;

        foreach (var batch in expired)
        {
            var qty = batch.QuantityRemaining;
            batch.IsActive = false;
            batch.QuantityRemaining = 0;

            _db.StockMovements.Add(new StockMovement
            {
                BatchId = batch.Id,
                MovementType = MovementType.Expired,
                Quantity = qty,
                Reference = $"AUTO-EXPIRY/{batch.BatchNumber}",
                PerformedById = systemUserId ?? "system",
                PerformedAt = now,
                Notes = "Auto-marked expired by background service"
            });
        }
        await _db.SaveChangesAsync(ct);
        return expired.Count;
    }

    private static object Snapshot(Batch b) => new
    {
        b.Id, b.DrugId, b.BatchNumber, b.SupplierId,
        b.QuantityIn, b.QuantityRemaining,
        CostPrice = b.CostPrice.ToString("0.##"),
        SellPrice = b.SellPrice.ToString("0.##"),
        b.ExpiryDate, b.ReceivedAt, b.IsActive
    };
}
