using FluentAssertions;
using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Services;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.UnitTests.Fakes;

namespace MediStock.UnitTests;

public class SaleServiceListAndSearchTests
{
    private static async Task SeedAndSell(MediStock.Infrastructure.Data.AppDbContext ctx, int qty)
    {
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.AddRange(
            new Drug { Id = 1, Name = "Panadol", Strength = "5", CategoryId = 1 },
            new Drug { Id = 2, Name = "Brufen", Strength = "5", CategoryId = 1, IsActive = false });
        ctx.Batches.Add(new Batch { Id = 10, DrugId = 1, SupplierId = 1, BatchNumber = "B",
                                    QuantityIn = 100, QuantityRemaining = 100, CostPrice = 100, SellPrice = 200,
                                    ExpiryDate = DateTime.UtcNow.AddMonths(6), IsActive = true });
        ctx.Users.Add(new AppUser { Id = "u1", UserName = "u1", FullName = "Cashier", Email = "u@x", IsActive = true });
        await ctx.SaveChangesAsync();

        if (qty > 0)
        {
            var svc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
            await svc.CompleteSaleAsync(new CheckoutDto
            {
                Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = qty } },
                PaymentMethod = PaymentMethod.Cash
            }, "u1", default);
        }
    }

    [Fact]
    public async Task SearchDrugs_ExcludesInactive_AndReturnsCurrentStock()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAndSell(ctx, 0);

        var svc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
        var results = await svc.SearchDrugsAsync(null);

        results.Should().HaveCount(1); // Brufen is inactive
        results[0].Name.Should().Be("Panadol");
        results[0].CurrentStock.Should().Be(100);
        results[0].UnitPrice.Should().Be(200m);
    }

    [Fact]
    public async Task List_OrderedByCompletedAtDesc_ByDefault()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAndSell(ctx, 0); // seed only

        var generator = new FakeReceiptGen();
        var saleSvc = new SaleService(ctx, generator, new FakePdf(), new FakeAudit());

        await saleSvc.CompleteSaleAsync(new CheckoutDto
        {
            Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = 1 } },
            PaymentMethod = PaymentMethod.Cash
        }, "u1", default);

        await Task.Delay(20);

        await saleSvc.CompleteSaleAsync(new CheckoutDto
        {
            Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = 2 } },
            PaymentMethod = PaymentMethod.Cash
        }, "u1", default);

        var page = await saleSvc.ListAsync(new PageRequest { Page = 1, PageSize = 10 });

        page.TotalCount.Should().Be(2);
        page.Items.Select(i => i.CompletedAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetById_IncludesLines()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAndSell(ctx, 3);

        var svc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
        var first = (await svc.ListAsync(new PageRequest())).Items.First();
        var detail = await svc.GetByIdAsync(first.Id);

        detail.Should().NotBeNull();
        detail!.Lines.Should().HaveCount(1);
        detail.Lines[0].Quantity.Should().Be(3);
        detail.Lines[0].DrugName.Should().Be("Panadol");
    }

    private static async Task SeedAndSell_AdditionalSale(MediStock.Infrastructure.Data.AppDbContext ctx)
    {
        var svc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
        await svc.CompleteSaleAsync(new CheckoutDto
        {
            Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = 2 } },
            PaymentMethod = PaymentMethod.Cash
        }, "u1", default);
    }
}
