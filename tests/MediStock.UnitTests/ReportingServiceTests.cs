using FluentAssertions;
using MediStock.Application.Dtos;
using MediStock.Application.Services;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.UnitTests.Fakes;

namespace MediStock.UnitTests;

public class ReportingServiceTests
{
    private static async Task<int> SeedAndSellAsync(MediStock.Infrastructure.Data.AppDbContext ctx)
    {
        ctx.Categories.Add(new Category { Id = 1, Name = "Cat" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "Sup" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "Panadol", Strength = "5", CategoryId = 1 });
        ctx.Batches.Add(new Batch { Id = 10, DrugId = 1, SupplierId = 1, BatchNumber = "B",
                                    QuantityIn = 100, QuantityRemaining = 100, CostPrice = 100, SellPrice = 200,
                                    ExpiryDate = DateTime.UtcNow.AddMonths(6), IsActive = true });
        ctx.Users.Add(new AppUser { Id = "u1", UserName = "u1", FullName = "U", Email = "u@x", IsActive = true });
        await ctx.SaveChangesAsync();

        var sale = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
        var r = await sale.CompleteSaleAsync(new CheckoutDto
        {
            Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = 10 } },
            PaymentMethod = PaymentMethod.Cash
        }, "u1", default);
        return r.SaleId;
    }

    [Fact]
    public async Task DailyRevenue_PadsMissingDays()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAndSellAsync(ctx);

        var svc = new ReportingService(ctx);
        var data = await svc.GetDailyRevenueAsync(7);

        data.Should().HaveCount(7);
        data.Sum(p => p.TransactionCount).Should().Be(1);
        data.Sum(p => p.Revenue).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TopDrugs_ReturnsRevenueForRange()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAndSellAsync(ctx);

        var svc = new ReportingService(ctx);
        var top = await svc.GetTopDrugsAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), 5);

        top.Should().HaveCount(1);
        top[0].DrugName.Should().Be("Panadol");
        top[0].QuantitySold.Should().Be(10);
        top[0].Revenue.Should().Be(2000m);
    }

    [Fact]
    public async Task ProfitMargin_ComputesCorrectly()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAndSellAsync(ctx);

        var svc = new ReportingService(ctx);
        var rows = await svc.GetProfitMarginAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        var row = rows.Should().ContainSingle().Subject;
        row.Revenue.Should().Be(2000m);
        row.Cost.Should().Be(1000m);
        row.Profit.Should().Be(1000m);
        row.MarginPct.Should().Be(50m);
    }

    [Fact]
    public async Task StockValuation_AggregatesByCategory()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAndSellAsync(ctx); // sells 10, leaving 90 in batch 10

        var svc = new ReportingService(ctx);
        var v = await svc.GetStockValuationAsync();

        // 90 remaining * 100 cost = 9000
        v.TotalValue.Should().Be(9000m);
        v.TotalUnits.Should().Be(90);
        v.ByCategory.Should().ContainSingle();
    }

    [Fact]
    public async Task SalesReport_ExcludesVoidedFromTotals()
    {
        using var ctx = TestDb.CreateInMemory();
        var saleId = await SeedAndSellAsync(ctx);

        var saleSvc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
        await saleSvc.VoidSaleAsync(saleId, "u1", "test", default);

        var svc = new ReportingService(ctx);
        var report = await svc.GetSalesReportAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        report.Rows.Should().ContainSingle(r => r.IsVoided);
        report.TransactionCount.Should().Be(0);
        report.Total.Should().Be(0m);
    }
}
