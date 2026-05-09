using FluentAssertions;
using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Services;
using MediStock.UnitTests.Fakes;

namespace MediStock.UnitTests;

public class CustomerServiceTests
{
    [Fact]
    public async Task Create_TrimsWhitespace_AndAudits()
    {
        using var ctx = TestDb.CreateInMemory();
        var audit = new FakeAudit();
        var svc = new CustomerService(ctx, audit);

        var id = await svc.CreateAsync(new CustomerCreateDto
        {
            FullName = "  Mary Nakato  ", Phone = " 0700 ", IsRecurring = true
        });

        var c = await svc.GetByIdAsync(id);
        c!.FullName.Should().Be("Mary Nakato");
        c.Phone.Should().Be("0700");
        c.IsRecurring.Should().BeTrue();
        audit.Calls.Should().ContainSingle(call => call.EntityName == "Customer" && call.Action == "Create");
    }

    [Fact]
    public async Task List_SearchesAcrossNamePhoneNin()
    {
        using var ctx = TestDb.CreateInMemory();
        var svc = new CustomerService(ctx, new FakeAudit());
        await svc.CreateAsync(new CustomerCreateDto { FullName = "Alice", NIN = "CM1" });
        await svc.CreateAsync(new CustomerCreateDto { FullName = "Bob", Phone = "555" });
        await svc.CreateAsync(new CustomerCreateDto { FullName = "Carol", NIN = "ZZZ" });

        (await svc.ListAsync(new PageRequest { Search = "Bob" })).TotalCount.Should().Be(1);
        (await svc.ListAsync(new PageRequest { Search = "555" })).TotalCount.Should().Be(1);
        (await svc.ListAsync(new PageRequest { Search = "ZZZ" })).TotalCount.Should().Be(1);
        (await svc.ListAsync(new PageRequest { Search = "" })).TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        using var ctx = TestDb.CreateInMemory();
        var svc = new CustomerService(ctx, new FakeAudit());
        var id = await svc.CreateAsync(new CustomerCreateDto { FullName = "Old" });

        await svc.UpdateAsync(new CustomerUpdateDto { Id = id, FullName = "New", IsRecurring = true });

        var c = await svc.GetByIdAsync(id);
        c!.FullName.Should().Be("New");
        c.IsRecurring.Should().BeTrue();
    }
}
