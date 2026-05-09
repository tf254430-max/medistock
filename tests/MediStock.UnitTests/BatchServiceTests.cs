using FluentAssertions;
using MediStock.Application.Dtos;
using MediStock.Application.Services;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.UnitTests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MediStock.UnitTests;

public class BatchServiceTests
{
    private static IHttpContextAccessor BuildAccessor(string userId)
    {
        var ctx = new DefaultHttpContext();
        ctx.Items[AuditService.UserIdItemKey] = userId;
        return new HttpContextAccessorStub(ctx);
    }

    [Fact]
    public async Task CreateBatch_AddsStockInMovement()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "Panadol", Strength = "500mg", CategoryId = 1 });
        ctx.Users.Add(new AppUser { Id = "u1", UserName = "u1", FullName = "U", Email = "u@x", IsActive = true });
        await ctx.SaveChangesAsync();

        var svc = new BatchService(ctx, new FakeAudit(), BuildAccessor("u1"));
        var batchId = await svc.CreateAsync(new BatchCreateDto
        {
            DrugId = 1, SupplierId = 1, BatchNumber = "BN-1",
            Quantity = 50, CostPrice = 100, SellPrice = 200,
            ExpiryDate = DateTime.UtcNow.AddMonths(12)
        });

        var batch = await ctx.Batches.FindAsync(batchId);
        batch!.QuantityIn.Should().Be(50);
        batch.QuantityRemaining.Should().Be(50);

        var movement = await ctx.StockMovements.FirstOrDefaultAsync(m => m.BatchId == batchId);
        movement.Should().NotBeNull();
        movement!.MovementType.Should().Be(MovementType.In);
        movement.Quantity.Should().Be(50);
    }

    [Fact]
    public async Task MarkExpired_DeactivatesAndAddsExpiredMovement()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "Panadol", Strength = "500mg", CategoryId = 1 });
        ctx.Users.Add(new AppUser { Id = "u1", UserName = "u1", FullName = "U", Email = "u@x", IsActive = true });
        ctx.Batches.AddRange(
            new Batch { Id = 10, DrugId = 1, SupplierId = 1, BatchNumber = "OLD",
                        QuantityIn = 5, QuantityRemaining = 5, SellPrice = 100, CostPrice = 50,
                        ExpiryDate = DateTime.UtcNow.AddDays(-1), IsActive = true },
            new Batch { Id = 11, DrugId = 1, SupplierId = 1, BatchNumber = "NEW",
                        QuantityIn = 5, QuantityRemaining = 5, SellPrice = 100, CostPrice = 50,
                        ExpiryDate = DateTime.UtcNow.AddDays(20), IsActive = true });
        await ctx.SaveChangesAsync();

        var svc = new BatchService(ctx, new FakeAudit(), BuildAccessor("u1"));
        var count = await svc.MarkExpiredAsync();

        count.Should().Be(1);
        var expired = await ctx.Batches.FindAsync(10);
        expired!.IsActive.Should().BeFalse();
        expired.QuantityRemaining.Should().Be(0);

        var fresh = await ctx.Batches.FindAsync(11);
        fresh!.IsActive.Should().BeTrue();

        var mvt = await ctx.StockMovements.FirstOrDefaultAsync(m => m.BatchId == 10);
        mvt.Should().NotBeNull();
        mvt!.MovementType.Should().Be(MovementType.Expired);
        mvt.Quantity.Should().Be(5);
    }

    private class HttpContextAccessorStub : IHttpContextAccessor
    {
        public HttpContextAccessorStub(HttpContext ctx) { HttpContext = ctx; }
        public HttpContext? HttpContext { get; set; }
    }
}
