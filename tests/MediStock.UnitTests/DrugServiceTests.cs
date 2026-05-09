using FluentAssertions;
using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Services;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.UnitTests.Fakes;

namespace MediStock.UnitTests;

public class DrugServiceTests
{
    private static async Task SeedAsync(MediStock.Infrastructure.Data.AppDbContext ctx)
    {
        ctx.Categories.AddRange(
            new Category { Id = 1, Name = "Analgesics" },
            new Category { Id = 2, Name = "Antibiotics" });
        ctx.Drugs.AddRange(
            new Drug { Id = 1, Name = "Panadol", GenericName = "Paracetamol", CategoryId = 1, Strength = "500mg", Form = DosageForm.Tablet, ReorderLevel = 50, IsActive = true },
            new Drug { Id = 2, Name = "Brufen", GenericName = "Ibuprofen", CategoryId = 1, Strength = "400mg", Form = DosageForm.Tablet, ReorderLevel = 40, IsActive = true },
            new Drug { Id = 3, Name = "Amoxil", GenericName = "Amoxicillin", CategoryId = 2, Strength = "500mg", Form = DosageForm.Capsule, ReorderLevel = 30, IsActive = true });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task List_WithSearch_ReturnsMatching()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAsync(ctx);
        var svc = new DrugService(ctx, new FakeAudit());

        var result = await svc.ListAsync(new PageRequest { Search = "Amox" });

        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Amoxil");
    }

    [Fact]
    public async Task List_WithSorting_OrdersAccordingly()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAsync(ctx);
        var svc = new DrugService(ctx, new FakeAudit());

        var asc = await svc.ListAsync(new PageRequest { Sort = "name", Descending = false });
        asc.Items.Select(i => i.Name).Should().BeInAscendingOrder();

        var desc = await svc.ListAsync(new PageRequest { Sort = "name", Descending = true });
        desc.Items.Select(i => i.Name).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task List_WithPaging_LimitsResults()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAsync(ctx);
        var svc = new DrugService(ctx, new FakeAudit());

        var page1 = await svc.ListAsync(new PageRequest { Page = 1, PageSize = 2 });
        var page2 = await svc.ListAsync(new PageRequest { Page = 2, PageSize = 2 });

        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(1);
        page1.TotalCount.Should().Be(3);
        page1.HasNext.Should().BeTrue();
        page2.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_FlipsIsActive_AndAuditsOnce()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAsync(ctx);
        var audit = new FakeAudit();
        var svc = new DrugService(ctx, audit);

        await svc.DeactivateAsync(1);

        var d = await ctx.Drugs.FindAsync(1);
        d!.IsActive.Should().BeFalse();
        audit.Calls.Should().ContainSingle(c => c.EntityName == "Drug" && c.Action == "Deactivate");

        // Idempotent — second call doesn't create a second audit row.
        await svc.DeactivateAsync(1);
        audit.Calls.Count(c => c.EntityName == "Drug" && c.Action == "Deactivate").Should().Be(1);
    }

    [Fact]
    public async Task Create_ThenGetById_ReturnsDetail()
    {
        using var ctx = TestDb.CreateInMemory();
        await SeedAsync(ctx);
        var svc = new DrugService(ctx, new FakeAudit());

        var id = await svc.CreateAsync(new DrugCreateDto
        {
            Name = "New", CategoryId = 1, Form = DosageForm.Tablet, Strength = "10mg", ReorderLevel = 5
        });

        var d = await svc.GetByIdAsync(id);
        d.Should().NotBeNull();
        d!.Name.Should().Be("New");
        d.IsActive.Should().BeTrue();
        d.CurrentStock.Should().Be(0);
    }
}
