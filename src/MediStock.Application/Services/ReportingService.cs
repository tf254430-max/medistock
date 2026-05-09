using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Enums;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class ReportingService : IReportingService
{
    private readonly AppDbContext _db;
    public ReportingService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DailyRevenuePoint>> GetDailyRevenueAsync(int days, CancellationToken ct = default)
    {
        var to = DateTime.UtcNow.Date.AddDays(1);
        var from = to.AddDays(-Math.Max(1, days));

        var raw = await _db.Sales.AsNoTracking()
            .Where(s => !s.IsVoided && s.CompletedAt >= from && s.CompletedAt < to)
            .Select(s => new { s.CompletedAt.Date, s.Total })
            .ToListAsync(ct);

        var grouped = raw
            .GroupBy(r => r.Date)
            .Select(g => new DailyRevenuePoint
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.Total),
                TransactionCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        // Pad missing days with zero rows so the chart line shows continuous days.
        var byDate = grouped.ToDictionary(x => x.Date);
        var result = new List<DailyRevenuePoint>();
        for (var d = from; d < to; d = d.AddDays(1))
        {
            result.Add(byDate.TryGetValue(d, out var p) ? p : new DailyRevenuePoint { Date = d });
        }
        return result;
    }

    public async Task<IReadOnlyList<TopDrugRow>> GetTopDrugsAsync(DateTime from, DateTime to, int limit, CancellationToken ct = default)
    {
        return await _db.SaleItems.AsNoTracking()
            .Where(i => !i.Sale.IsVoided && i.Sale.CompletedAt >= from && i.Sale.CompletedAt < to)
            .GroupBy(i => new { i.DrugId, i.Drug.Name })
            .Select(g => new TopDrugRow
            {
                DrugId = g.Key.DrugId,
                DrugName = g.Key.Name,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(r => r.Revenue)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProfitMarginRow>> GetProfitMarginAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var raw = await _db.SaleItems.AsNoTracking()
            .Where(i => !i.Sale.IsVoided && i.Sale.CompletedAt >= from && i.Sale.CompletedAt < to)
            .Select(i => new
            {
                i.DrugId,
                DrugName = i.Drug.Name,
                i.Quantity,
                i.LineTotal,
                Cost = i.Batch.CostPrice * i.Quantity
            })
            .ToListAsync(ct);

        return raw
            .GroupBy(r => new { r.DrugId, r.DrugName })
            .Select(g => new ProfitMarginRow
            {
                DrugId = g.Key.DrugId,
                DrugName = g.Key.DrugName,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal),
                Cost = g.Sum(x => x.Cost)
            })
            .OrderByDescending(r => r.Profit)
            .ToList();
    }

    public async Task<IReadOnlyList<SupplierSpendRow>> GetSupplierSpendAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var raw = await _db.Batches.AsNoTracking()
            .Where(b => b.ReceivedAt >= from && b.ReceivedAt < to)
            .Select(b => new
            {
                b.SupplierId, SupplierName = b.Supplier.Name,
                b.QuantityIn, b.CostPrice
            })
            .ToListAsync(ct);

        return raw
            .GroupBy(b => new { b.SupplierId, b.SupplierName })
            .Select(g => new SupplierSpendRow
            {
                SupplierId = g.Key.SupplierId,
                SupplierName = g.Key.SupplierName,
                BatchCount = g.Count(),
                UnitsReceived = g.Sum(x => x.QuantityIn),
                TotalSpend = g.Sum(x => x.CostPrice * x.QuantityIn)
            })
            .OrderByDescending(r => r.TotalSpend)
            .ToList();
    }

    public async Task<StockValuationDto> GetStockValuationAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var raw = await _db.Batches.AsNoTracking()
            .Where(b => b.IsActive && b.QuantityRemaining > 0 && b.ExpiryDate > now)
            .Select(b => new
            {
                b.Drug.CategoryId,
                CategoryName = b.Drug.Category.Name,
                b.QuantityRemaining,
                b.CostPrice
            })
            .ToListAsync(ct);

        var byCat = raw
            .GroupBy(x => new { x.CategoryId, x.CategoryName })
            .Select(g => new StockValuationCategoryRow
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                UnitsOnHand = g.Sum(x => x.QuantityRemaining),
                Value = g.Sum(x => x.CostPrice * x.QuantityRemaining)
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        return new StockValuationDto
        {
            ByCategory = byCat,
            TotalUnits = byCat.Sum(x => x.UnitsOnHand),
            TotalValue = byCat.Sum(x => x.Value)
        };
    }

    public async Task<SalesReportSummary> GetSalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.Sales.AsNoTracking()
            .Where(s => s.CompletedAt >= from && s.CompletedAt < to)
            .OrderBy(s => s.CompletedAt)
            .Select(s => new SalesReportRow
            {
                Id = s.Id,
                ReceiptNumber = s.ReceiptNumber,
                CompletedAt = s.CompletedAt,
                CashierName = s.Cashier.FullName,
                CustomerName = s.Customer != null ? s.Customer.FullName : null,
                PaymentMethod = s.PaymentMethod.ToString(),
                Items = s.Items.Count,
                SubTotal = s.SubTotal,
                Discount = s.Discount,
                VatAmount = s.VatAmount,
                Total = s.Total,
                IsVoided = s.IsVoided
            })
            .ToListAsync(ct);

        var nonVoided = rows.Where(r => !r.IsVoided).ToList();
        return new SalesReportSummary
        {
            From = from,
            To = to,
            Rows = rows,
            TransactionCount = nonVoided.Count,
            SubTotal = nonVoided.Sum(r => r.SubTotal),
            Discount = nonVoided.Sum(r => r.Discount),
            Vat = nonVoided.Sum(r => r.VatAmount),
            Total = nonVoided.Sum(r => r.Total)
        };
    }
}
