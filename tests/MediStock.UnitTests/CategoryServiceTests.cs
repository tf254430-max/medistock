using FluentAssertions;
using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Services;
using MediStock.UnitTests.Fakes;

namespace MediStock.UnitTests;

public class CategoryServiceTests
{
    [Fact]
    public async Task Create_PersistsAndReturnsId()
    {
        using var ctx = TestDb.CreateInMemory();
        var svc = new CategoryService(ctx, new FakeAudit());

        var id = await svc.CreateAsync(new CategoryCreateDto
        {
            Name = " Analgesics ", Description = "Pain relief"
        });

        var c = await svc.GetByIdAsync(id);
        c.Should().NotBeNull();
        c!.Name.Should().Be("Analgesics");
        c.Description.Should().Be("Pain relief");
    }

    [Fact]
    public async Task Update_ChangesNameAndDescription()
    {
        using var ctx = TestDb.CreateInMemory();
        var audit = new FakeAudit();
        var svc = new CategoryService(ctx, audit);
        var id = await svc.CreateAsync(new CategoryCreateDto { Name = "Old" });

        await svc.UpdateAsync(new CategoryUpdateDto { Id = id, Name = "New", Description = "x" });

        var c = await svc.GetByIdAsync(id);
        c!.Name.Should().Be("New");
        c.Description.Should().Be("x");
        audit.Calls.Should().Contain(call => call.Action == "Update" && call.EntityName == "Category");
    }

    [Fact]
    public async Task List_FiltersBySearch_AndPages()
    {
        using var ctx = TestDb.CreateInMemory();
        var svc = new CategoryService(ctx, new FakeAudit());
        await svc.CreateAsync(new CategoryCreateDto { Name = "Analgesics" });
        await svc.CreateAsync(new CategoryCreateDto { Name = "Antibiotics" });
        await svc.CreateAsync(new CategoryCreateDto { Name = "Vitamins" });

        var search = await svc.ListAsync(new PageRequest { Search = "Anti" });
        search.TotalCount.Should().Be(1);
        search.Items[0].Name.Should().Be("Antibiotics");

        var paged = await svc.ListAsync(new PageRequest { PageSize = 2 });
        paged.Items.Should().HaveCount(2);
        paged.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task ListAll_ReturnsEverythingOrderedByName()
    {
        using var ctx = TestDb.CreateInMemory();
        var svc = new CategoryService(ctx, new FakeAudit());
        await svc.CreateAsync(new CategoryCreateDto { Name = "Zeta" });
        await svc.CreateAsync(new CategoryCreateDto { Name = "Alpha" });

        var all = await svc.ListAllAsync();
        all.Select(x => x.Name).Should().BeInAscendingOrder();
    }
}
