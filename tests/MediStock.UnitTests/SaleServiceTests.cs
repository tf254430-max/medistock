using FluentAssertions;
using MediStock.Application.Dtos;
using MediStock.Application.Services;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.Domain.Exceptions;
using MediStock.UnitTests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace MediStock.UnitTests;

public class SaleServiceTests
{
    [Fact]
    public async Task CompleteSale_WithSufficientStock_DecrementsFifoByExpiry()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "Panadol", Strength = "500mg", CategoryId = 1 });
        ctx.Batches.AddRange(
            new Batch { Id = 10, DrugId = 1, SupplierId = 1, BatchNumber = "B10",
                        QuantityIn = 20, QuantityRemaining = 20, SellPrice = 200, CostPrice = 100,
                        ExpiryDate = DateTime.UtcNow.AddDays(20), IsActive = true },
            new Batch { Id = 11, DrugId = 1, SupplierId = 1, BatchNumber = "B11",
                        QuantityIn = 20, QuantityRemaining = 20, SellPrice = 200, CostPrice = 100,
                        ExpiryDate = DateTime.UtcNow.AddDays(60), IsActive = true });
        ctx.Users.Add(new AppUser { Id = "user-1", UserName = "u1", FullName = "U", Email = "u@x", IsActive = true });
        await ctx.SaveChangesAsync();

        var svc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
        var dto = new CheckoutDto
        {
            Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = 25 } },
            PaymentMethod = PaymentMethod.Cash
        };

        var result = await svc.CompleteSaleAsync(dto, "user-1", default);

        var b10 = await ctx.Batches.FindAsync(10);
        var b11 = await ctx.Batches.FindAsync(11);
        b10!.QuantityRemaining.Should().Be(0);
        b11!.QuantityRemaining.Should().Be(15);
        result.ReceiptNumber.Should().StartWith("R-TEST-");
    }

    [Fact]
    public async Task CompleteSale_WithInsufficientStock_Throws()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "Panadol", Strength = "500mg", CategoryId = 1 });
        ctx.Batches.Add(new Batch { Id = 10, DrugId = 1, SupplierId = 1, BatchNumber = "B10",
                                    QuantityIn = 5, QuantityRemaining = 5, SellPrice = 200, CostPrice = 100,
                                    ExpiryDate = DateTime.UtcNow.AddDays(20), IsActive = true });
        ctx.Users.Add(new AppUser { Id = "user-1", UserName = "u1", FullName = "U", Email = "u@x", IsActive = true });
        await ctx.SaveChangesAsync();

        var svc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
        var dto = new CheckoutDto
        {
            Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = 100 } },
            PaymentMethod = PaymentMethod.Cash
        };

        var act = async () => await svc.CompleteSaleAsync(dto, "user-1", default);
        await act.Should().ThrowAsync<InsufficientStockException>();
    }

    [Fact]
    public async Task VoidSale_RestoresStock_AndAudits()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "Panadol", Strength = "500mg", CategoryId = 1 });
        ctx.Batches.Add(new Batch { Id = 10, DrugId = 1, SupplierId = 1, BatchNumber = "B10",
                                    QuantityIn = 20, QuantityRemaining = 20, SellPrice = 200, CostPrice = 100,
                                    ExpiryDate = DateTime.UtcNow.AddDays(20), IsActive = true });
        ctx.Users.Add(new AppUser { Id = "user-1", UserName = "u1", FullName = "U", Email = "u@x", IsActive = true });
        await ctx.SaveChangesAsync();

        var audit = new FakeAudit();
        var svc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), audit);
        var dto = new CheckoutDto
        {
            Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = 5 } },
            PaymentMethod = PaymentMethod.Cash
        };
        var sale = await svc.CompleteSaleAsync(dto, "user-1", default);

        await svc.VoidSaleAsync(sale.SaleId, "user-1", "test void", default);

        var b10 = await ctx.Batches.FindAsync(10);
        b10!.QuantityRemaining.Should().Be(20); // restored from 15
        var saleRow = await ctx.Sales.FindAsync(sale.SaleId);
        saleRow!.IsVoided.Should().BeTrue();
        saleRow.VoidReason.Should().Be("test void");
        audit.Calls.Should().Contain(c => c.EntityName == "Sale" && c.Action == "Void");
    }

    [Fact]
    public async Task ValidateCart_PrescriptionDrug_WithoutPrescription_ReturnsError()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "Amoxil", Strength = "500mg", CategoryId = 1, RequiresPrescription = true });
        await ctx.SaveChangesAsync();

        var svc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
        var dto = new CheckoutDto
        {
            PrescriptionId = null,
            Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = 1 } },
            PaymentMethod = PaymentMethod.Cash
        };

        var errors = await svc.ValidateCartAsync(dto, default);
        errors.Should().HaveCount(1);
        errors[0].Reason.Should().Contain("Prescription required");
    }

    [Fact]
    public async Task CompleteSale_WithLinkedPrescription_FlipsToDispensed()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Suppliers.Add(new Supplier { Id = 1, Name = "S" });
        ctx.Drugs.Add(new Drug { Id = 1, Name = "Panadol", Strength = "500mg", CategoryId = 1 });
        ctx.Batches.Add(new Batch { Id = 10, DrugId = 1, SupplierId = 1, BatchNumber = "B10",
                                    QuantityIn = 20, QuantityRemaining = 20, SellPrice = 200, CostPrice = 100,
                                    ExpiryDate = DateTime.UtcNow.AddDays(20), IsActive = true });
        ctx.Users.Add(new AppUser { Id = "user-1", UserName = "u1", FullName = "U", Email = "u@x", IsActive = true });
        ctx.Customers.Add(new Customer { Id = 1, FullName = "C" });
        ctx.Prescriptions.Add(new Prescription
        {
            Id = 100, CustomerId = 1, DoctorName = "Dr Test", IssuedDate = DateTime.UtcNow,
            Status = PrescriptionStatus.Issued, CreatedById = "user-1"
        });
        await ctx.SaveChangesAsync();

        var svc = new SaleService(ctx, new FakeReceiptGen(), new FakePdf(), new FakeAudit());
        var dto = new CheckoutDto
        {
            PrescriptionId = 100,
            Lines = new List<CheckoutLineDto> { new() { DrugId = 1, Quantity = 1 } },
            PaymentMethod = PaymentMethod.Cash
        };
        await svc.CompleteSaleAsync(dto, "user-1", default);

        var prx = await ctx.Prescriptions.FindAsync(100);
        prx!.Status.Should().Be(PrescriptionStatus.Dispensed);
    }
}
