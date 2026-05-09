using FluentAssertions;
using MediStock.Application.Services;
using MediStock.Domain.Entities;

namespace MediStock.UnitTests;

public class InventoryAlertServiceTests
{
    [Fact]
    public async Task GetLowStock_ReturnsDrugsAtOrBelowReorderLevel()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.AddRange(
            new Drug { Id = 1, Name = "LowStock", Strength = "5mg", CategoryId = 1, ReorderLevel = 50, IsActive = true },
            new Drug { Id = 2, Name = "PlentyStock", Strength = "5mg", CategoryId = 1, ReorderLevel = 10, IsActive = true });
        ctx.Batches.AddRange(
            new Batch { Id = 10, DrugId = 1, SupplierId = 1, BatchNumber = "A",
                        QuantityIn = 30, QuantityRemaining = 30, CostPrice = 1, SellPrice = 2,
                        ExpiryDate = DateTime.UtcNow.AddMonths(6), IsActive = true },
            new Batch { Id = 20, DrugId = 2, SupplierId = 1, BatchNumber = "B",
                        QuantityIn = 200, QuantityRemaining = 200, CostPrice = 1, SellPrice = 2,
                        ExpiryDate = DateTime.UtcNow.AddMonths(6), IsActive = true });
        await ctx.SaveChangesAsync();

        var svc = new InventoryAlertService(ctx);
        var low = await svc.GetLowStockAsync();

        low.Should().HaveCount(1);
        low[0].DrugId.Should().Be(1);
        low[0].CurrentStock.Should().Be(30);
        low[0].ReorderLevel.Should().Be(50);
    }

    [Fact]
    public async Task GetExpiringSoon_OnlyReturnsActiveBatchesWithinWindow()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "D", Strength = "5mg", CategoryId = 1, IsActive = true });
        ctx.Batches.AddRange(
            new Batch { Id = 1, DrugId = 1, SupplierId = 1, BatchNumber = "FAR",
                        QuantityIn = 10, QuantityRemaining = 10, CostPrice = 1, SellPrice = 2,
                        ExpiryDate = DateTime.UtcNow.AddDays(120), IsActive = true },
            new Batch { Id = 2, DrugId = 1, SupplierId = 1, BatchNumber = "SOON",
                        QuantityIn = 10, QuantityRemaining = 10, CostPrice = 1, SellPrice = 2,
                        ExpiryDate = DateTime.UtcNow.AddDays(15), IsActive = true },
            new Batch { Id = 3, DrugId = 1, SupplierId = 1, BatchNumber = "OLD",
                        QuantityIn = 10, QuantityRemaining = 10, CostPrice = 1, SellPrice = 2,
                        ExpiryDate = DateTime.UtcNow.AddDays(-2), IsActive = true });
        await ctx.SaveChangesAsync();

        var svc = new InventoryAlertService(ctx);
        var expiring = await svc.GetExpiringSoonAsync(30);

        expiring.Should().HaveCount(1);
        expiring[0].BatchNumber.Should().Be("SOON");
        expiring[0].DaysUntilExpiry.Should().BeInRange(13, 16);
    }

    [Fact]
    public async Task GetSummary_AddsLowStockAndExpiringCounts()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "D", Strength = "5mg", CategoryId = 1, ReorderLevel = 100, IsActive = true });
        ctx.Batches.Add(new Batch { Id = 1, DrugId = 1, SupplierId = 1, BatchNumber = "X",
                                    QuantityIn = 5, QuantityRemaining = 5, CostPrice = 1, SellPrice = 2,
                                    ExpiryDate = DateTime.UtcNow.AddDays(10), IsActive = true });
        await ctx.SaveChangesAsync();

        var svc = new InventoryAlertService(ctx);
        var summary = await svc.GetSummaryAsync();

        summary.LowStockCount.Should().Be(1);
        summary.ExpiringSoonCount.Should().Be(1);
        summary.Total.Should().Be(2);
    }
}
