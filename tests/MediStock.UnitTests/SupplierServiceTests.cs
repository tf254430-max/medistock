using FluentAssertions;
using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Services;
using MediStock.UnitTests.Fakes;

namespace MediStock.UnitTests;

public class SupplierServiceTests
{
    [Fact]
    public async Task Create_StoresAllFields_AndIsActive()
    {
        using var ctx = TestDb.CreateInMemory();
        var svc = new SupplierService(ctx, new FakeAudit());

        var id = await svc.CreateAsync(new SupplierCreateDto
        {
            Name = "S1", ContactPerson = "Joe", Phone = "+256...", Email = "x@y.ug",
            Address = "Road 1", Tin = "TIN1"
        });

        var s = await svc.GetByIdAsync(id);
        s.Should().NotBeNull();
        s!.Name.Should().Be("S1");
        s.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ModifiesFields_AndAudits()
    {
        using var ctx = TestDb.CreateInMemory();
        var audit = new FakeAudit();
        var svc = new SupplierService(ctx, audit);
        var id = await svc.CreateAsync(new SupplierCreateDto { Name = "Old" });

        await svc.UpdateAsync(new SupplierUpdateDto { Id = id, Name = "New", Phone = "p" });

        var s = await svc.GetByIdAsync(id);
        s!.Name.Should().Be("New");
        s.Phone.Should().Be("p");
        audit.Calls.Should().Contain(c => c.Action == "Update" && c.EntityName == "Supplier");
    }

    [Fact]
    public async Task Deactivate_HidesFromListAllActive()
    {
        using var ctx = TestDb.CreateInMemory();
        var svc = new SupplierService(ctx, new FakeAudit());
        var id = await svc.CreateAsync(new SupplierCreateDto { Name = "Gone" });

        await svc.DeactivateAsync(id);

        var active = await svc.ListAllActiveAsync();
        active.Should().NotContain(s => s.Id == id);

        var detail = await svc.GetByIdAsync(id);
        detail!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task List_Search_MatchesNameOrContactOrPhone()
    {
        using var ctx = TestDb.CreateInMemory();
        var svc = new SupplierService(ctx, new FakeAudit());
        await svc.CreateAsync(new SupplierCreateDto { Name = "Alpha", ContactPerson = "Joe" });
        await svc.CreateAsync(new SupplierCreateDto { Name = "Beta", ContactPerson = "Mary", Phone = "+256-700-XYZ" });
        await svc.CreateAsync(new SupplierCreateDto { Name = "Gamma" });

        (await svc.ListAsync(new PageRequest { Search = "Beta" })).TotalCount.Should().Be(1);
        (await svc.ListAsync(new PageRequest { Search = "Joe" })).TotalCount.Should().Be(1);
        (await svc.ListAsync(new PageRequest { Search = "XYZ" })).TotalCount.Should().Be(1);
    }
}
