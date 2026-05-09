using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.Domain.Exceptions;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class SaleService : ISaleService
{
    private readonly AppDbContext _db;
    private readonly IReceiptNumberGenerator _receiptNumberGen;
    private readonly IReceiptPdfService _receiptPdf;
    private readonly IAuditService _audit;

    public SaleService(
        AppDbContext db,
        IReceiptNumberGenerator receiptNumberGen,
        IReceiptPdfService receiptPdf,
        IAuditService audit)
    {
        _db = db;
        _receiptNumberGen = receiptNumberGen;
        _receiptPdf = receiptPdf;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CartValidationError>> ValidateCartAsync(CheckoutDto dto, CancellationToken ct = default)
    {
        var errors = new List<CartValidationError>();
        var now = DateTime.UtcNow;

        foreach (var line in dto.Lines)
        {
            if (line.Quantity <= 0) continue;

            var drug = await _db.Drugs.AsNoTracking()
                .Where(d => d.Id == line.DrugId)
                .Select(d => new { d.Id, d.Name, d.IsActive, d.RequiresPrescription })
                .FirstOrDefaultAsync(ct);

            if (drug is null)
            {
                errors.Add(new CartValidationError
                {
                    DrugId = line.DrugId, DrugName = "(unknown)", Reason = "Drug not found", Requested = line.Quantity
                });
                continue;
            }
            if (!drug.IsActive)
            {
                errors.Add(new CartValidationError
                {
                    DrugId = line.DrugId, DrugName = drug.Name, Reason = "Drug is inactive", Requested = line.Quantity
                });
                continue;
            }
            if (drug.RequiresPrescription && dto.PrescriptionId is null)
            {
                errors.Add(new CartValidationError
                {
                    DrugId = line.DrugId, DrugName = drug.Name,
                    Reason = "Prescription required for this drug",
                    Requested = line.Quantity
                });
                continue;
            }

            var available = await _db.Batches.AsNoTracking()
                .Where(b => b.DrugId == line.DrugId && b.IsActive
                         && b.QuantityRemaining > 0 && b.ExpiryDate > now)
                .SumAsync(b => b.QuantityRemaining, ct);

            if (available < line.Quantity)
            {
                errors.Add(new CartValidationError
                {
                    DrugId = line.DrugId, DrugName = drug.Name,
                    Reason = "Insufficient stock",
                    Requested = line.Quantity, Available = available
                });
            }
        }

        return errors;
    }

    public async Task<SaleResult> CompleteSaleAsync(CheckoutDto dto, string cashierId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var sale = new Sale
        {
            ReceiptNumber = await _receiptNumberGen.NextAsync(ct),
            CustomerId = dto.CustomerId,
            PrescriptionId = dto.PrescriptionId,
            CashierId = cashierId,
            PaymentMethod = dto.PaymentMethod,
            MobileMoneyRef = dto.MobileMoneyRef,
            CompletedAt = DateTime.UtcNow
        };

        decimal subtotal = 0m;

        foreach (var line in dto.Lines)
        {
            var batches = await _db.Batches
                .Where(b => b.DrugId == line.DrugId
                         && b.IsActive
                         && b.QuantityRemaining > 0
                         && b.ExpiryDate > DateTime.UtcNow)
                .OrderBy(b => b.ExpiryDate)            // FIFO by expiry
                .ToListAsync(ct);

            var needed = line.Quantity;
            if (batches.Sum(b => b.QuantityRemaining) < needed)
                throw new InsufficientStockException(line.DrugId);

            foreach (var batch in batches)
            {
                if (needed == 0) break;
                var take = Math.Min(needed, batch.QuantityRemaining);
                batch.QuantityRemaining -= take;
                needed -= take;

                sale.Items.Add(new SaleItem
                {
                    DrugId = line.DrugId,
                    BatchId = batch.Id,
                    Quantity = take,
                    UnitPrice = batch.SellPrice,
                    LineTotal = batch.SellPrice * take
                });

                _db.StockMovements.Add(new StockMovement
                {
                    BatchId = batch.Id,
                    MovementType = MovementType.Out,
                    Quantity = take,
                    Reference = sale.ReceiptNumber,
                    PerformedById = cashierId,
                    PerformedAt = DateTime.UtcNow
                });

                subtotal += batch.SellPrice * take;
            }
        }

        sale.SubTotal = subtotal;
        sale.Discount = dto.Discount;
        sale.VatAmount = Math.Round((subtotal - dto.Discount) * 0.18m, 2);
        sale.Total = subtotal - dto.Discount + sale.VatAmount;

        if (dto.PrescriptionId is int prxId)
        {
            var prescription = await _db.Prescriptions.FirstOrDefaultAsync(p => p.Id == prxId, ct);
            if (prescription is not null && prescription.Status != PrescriptionStatus.Dispensed)
                prescription.Status = PrescriptionStatus.Dispensed;
        }

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync(ct);

        var pdfPath = await _receiptPdf.GenerateAsync(sale, ct);
        await _audit.LogAsync("Sale", sale.Id.ToString(), "Create", null, AuditSnapshot(sale), ct);

        await tx.CommitAsync(ct);
        return new SaleResult(sale.Id, sale.ReceiptNumber, pdfPath);
    }

    public async Task VoidSaleAsync(int saleId, string adminUserId, string reason, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var sale = await _db.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == saleId, ct)
            ?? throw new KeyNotFoundException($"Sale {saleId} not found.");

        if (sale.IsVoided)
            throw new InvalidOperationException("Sale is already voided.");
        if ((DateTime.UtcNow - sale.CompletedAt).TotalMinutes > 30)
            throw new InvalidOperationException("Sales can only be voided within 30 minutes of completion.");

        var before = AuditSnapshot(sale);

        // Restore each batch's QuantityRemaining and write a Voided stock movement.
        foreach (var item in sale.Items)
        {
            var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == item.BatchId, ct);
            if (batch is null) continue;
            batch.QuantityRemaining += item.Quantity;

            _db.StockMovements.Add(new StockMovement
            {
                BatchId = batch.Id,
                MovementType = MovementType.Voided,
                Quantity = item.Quantity,
                Reference = $"VOID/{sale.ReceiptNumber}",
                PerformedById = adminUserId,
                PerformedAt = DateTime.UtcNow,
                Notes = string.IsNullOrWhiteSpace(reason) ? "Sale voided" : $"Voided: {reason}"
            });
        }

        sale.IsVoided = true;
        sale.VoidedAt = DateTime.UtcNow;
        sale.VoidedById = adminUserId;
        sale.VoidReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Sale", sale.Id.ToString(), "Void", before, AuditSnapshot(sale), ct);
        await tx.CommitAsync(ct);
    }

    public async Task<PagedResult<SaleListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default)
    {
        IQueryable<Sale> q = _db.Sales.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(x => EF.Functions.Like(x.ReceiptNumber, $"%{s}%")
                          || EF.Functions.Like(x.Cashier.FullName, $"%{s}%")
                          || (x.Customer != null && EF.Functions.Like(x.Customer.FullName, $"%{s}%")));
        }

        q = (request.Sort?.ToLowerInvariant(), request.Descending) switch
        {
            ("date", false) => q.OrderBy(x => x.CompletedAt),
            ("date", true) => q.OrderByDescending(x => x.CompletedAt),
            ("total", false) => q.OrderBy(x => x.Total),
            ("total", true) => q.OrderByDescending(x => x.Total),
            ("receipt", false) => q.OrderBy(x => x.ReceiptNumber),
            ("receipt", true) => q.OrderByDescending(x => x.ReceiptNumber),
            _ => q.OrderByDescending(x => x.CompletedAt)
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SaleListItemDto
            {
                Id = x.Id,
                ReceiptNumber = x.ReceiptNumber,
                CompletedAt = x.CompletedAt,
                CashierName = x.Cashier.FullName,
                CustomerName = x.Customer != null ? x.Customer.FullName : null,
                PaymentMethod = x.PaymentMethod,
                Total = x.Total,
                IsVoided = x.IsVoided,
                LineCount = x.Items.Count
            })
            .ToListAsync(ct);

        return new PagedResult<SaleListItemDto>
        {
            Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }

    public async Task<SaleDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var raw = await _db.Sales.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new
            {
                s.Id, s.ReceiptNumber, s.CompletedAt, CashierName = s.Cashier.FullName,
                CustomerName = s.Customer != null ? s.Customer.FullName : null,
                s.PaymentMethod, s.MobileMoneyRef,
                s.SubTotal, s.Discount, s.VatAmount, s.Total,
                s.IsVoided, s.VoidedAt, VoidedByName = s.VoidedBy != null ? s.VoidedBy.FullName : null, s.VoidReason,
                Lines = s.Items.Select(i => new SaleLineDto
                {
                    DrugId = i.DrugId,
                    DrugName = i.Drug.Name,
                    BatchId = i.BatchId,
                    BatchNumber = i.Batch.BatchNumber,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (raw is null) return null;

        return new SaleDetailDto
        {
            Id = raw.Id, ReceiptNumber = raw.ReceiptNumber, CompletedAt = raw.CompletedAt,
            CashierName = raw.CashierName, CustomerName = raw.CustomerName,
            PaymentMethod = raw.PaymentMethod, MobileMoneyRef = raw.MobileMoneyRef,
            SubTotal = raw.SubTotal, Discount = raw.Discount,
            VatAmount = raw.VatAmount, Total = raw.Total,
            IsVoided = raw.IsVoided, VoidedAt = raw.VoidedAt,
            VoidedByName = raw.VoidedByName, VoidReason = raw.VoidReason,
            Lines = raw.Lines
        };
    }

    public async Task<IReadOnlyList<DrugSearchResultDto>> SearchDrugsAsync(string? q, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        IQueryable<Drug> query = _db.Drugs.AsNoTracking().Where(d => d.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(d => EF.Functions.Like(d.Name, $"%{s}%")
                                  || (d.GenericName != null && EF.Functions.Like(d.GenericName, $"%{s}%"))
                                  || (d.Barcode != null && EF.Functions.Like(d.Barcode, $"%{s}%")));
        }

        return await query
            .OrderBy(d => d.Name)
            .Take(15)
            .Select(d => new DrugSearchResultDto
            {
                Id = d.Id,
                Name = d.Name,
                GenericName = d.GenericName,
                Strength = d.Strength,
                RequiresPrescription = d.RequiresPrescription,
                CurrentStock = d.Batches
                    .Where(b => b.IsActive && b.ExpiryDate > now)
                    .Sum(b => b.QuantityRemaining),
                UnitPrice = d.Batches
                    .Where(b => b.IsActive && b.QuantityRemaining > 0 && b.ExpiryDate > now)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => (decimal?)b.SellPrice)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);
    }

    private static object AuditSnapshot(Sale s) => new
    {
        s.Id, s.ReceiptNumber, s.CustomerId, s.PrescriptionId, s.CashierId,
        SubTotal = s.SubTotal.ToString("0.##"),
        Discount = s.Discount.ToString("0.##"),
        VatAmount = s.VatAmount.ToString("0.##"),
        Total = s.Total.ToString("0.##"),
        PaymentMethod = s.PaymentMethod.ToString(),
        s.MobileMoneyRef, s.CompletedAt,
        s.IsVoided, s.VoidedAt, s.VoidedById, s.VoidReason,
        Items = s.Items.Select(i => new
        {
            i.DrugId, i.BatchId, i.Quantity,
            UnitPrice = i.UnitPrice.ToString("0.##"),
            LineTotal = i.LineTotal.ToString("0.##")
        }).ToArray()
    };
}
