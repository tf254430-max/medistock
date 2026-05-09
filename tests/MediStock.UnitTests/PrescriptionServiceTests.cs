using FluentAssertions;
using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Services;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.UnitTests.Fakes;

namespace MediStock.UnitTests;

public class PrescriptionServiceTests
{
    private static async Task<int> SeedAsync(MediStock.Infrastructure.Data.AppDbContext ctx)
    {
        ctx.Categories.Add(new Category { Id = 1, Name = "X" });
        ctx.Drugs.AddRange(
            new Drug { Id = 1, Name = "D1", Strength = "5", CategoryId = 1, RequiresPrescription = true },
            new Drug { Id = 2, Name = "D2", Strength = "5", CategoryId = 1 });
        ctx.Customers.Add(new Customer { Id = 100, FullName = "Cust A" });
        ctx.Users.Add(new AppUser { Id = "u1", UserName = "u1", FullName = "Pharma", Email = "p@x", IsActive = true });
        await ctx.SaveChangesAsync();
        return 100;
    }

    [Fact]
    public async Task Create_PersistsLineItems_StatusIssued()
    {
        using var ctx = TestDb.CreateInMemory();
        var customerId = await SeedAsync(ctx);
        var svc = new PrescriptionService(ctx, new FakeAudit());

        var id = await svc.CreateAsync(new PrescriptionCreateDto
        {
            CustomerId = customerId,
            DoctorName = "Dr Test",
            IssuedDate = DateTime.UtcNow,
            Items = new List<PrescriptionItemCreateDto>
            {
                new() { DrugId = 1, Quantity = 10, Dosage = "1 x 3 daily" },
                new() { DrugId = 2, Quantity = 5, Dosage = "as needed" }
            }
        }, "u1");

        var p = await svc.GetByIdAsync(id);
        p.Should().NotBeNull();
        p!.Status.Should().Be(PrescriptionStatus.Issued);
        p.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Cancel_ChangesStatus_BlocksWhenAlreadyDispensed()
    {
        using var ctx = TestDb.CreateInMemory();
        var customerId = await SeedAsync(ctx);
        var svc = new PrescriptionService(ctx, new FakeAudit());
        var id = await svc.CreateAsync(new PrescriptionCreateDto
        {
            CustomerId = customerId,
            DoctorName = "Dr Test",
            IssuedDate = DateTime.UtcNow,
            Items = new List<PrescriptionItemCreateDto> { new() { DrugId = 1, Quantity = 1, Dosage = "x" } }
        }, "u1");

        await svc.CancelAsync(id);
        (await svc.GetByIdAsync(id))!.Status.Should().Be(PrescriptionStatus.Cancelled);

        // Force-dispense via service then try to cancel — should throw.
        var prx = await ctx.Prescriptions.FindAsync(id);
        prx!.Status = PrescriptionStatus.Dispensed;
        await ctx.SaveChangesAsync();

        var act = async () => await svc.CancelAsync(id);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MarkDispensed_IsIdempotent()
    {
        using var ctx = TestDb.CreateInMemory();
        var customerId = await SeedAsync(ctx);
        var audit = new FakeAudit();
        var svc = new PrescriptionService(ctx, audit);
        var id = await svc.CreateAsync(new PrescriptionCreateDto
        {
            CustomerId = customerId, DoctorName = "Dr",
            IssuedDate = DateTime.UtcNow,
            Items = new List<PrescriptionItemCreateDto> { new() { DrugId = 2, Quantity = 1, Dosage = "x" } }
        }, "u1");

        await svc.MarkDispensedAsync(id);
        await svc.MarkDispensedAsync(id);

        (await svc.GetByIdAsync(id))!.Status.Should().Be(PrescriptionStatus.Dispensed);
        audit.Calls.Count(c => c.EntityName == "Prescription" && c.Action == "Dispense").Should().Be(1);
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        using var ctx = TestDb.CreateInMemory();
        var customerId = await SeedAsync(ctx);
        var svc = new PrescriptionService(ctx, new FakeAudit());
        var id1 = await svc.CreateAsync(new PrescriptionCreateDto
        {
            CustomerId = customerId, DoctorName = "A", IssuedDate = DateTime.UtcNow,
            Items = new List<PrescriptionItemCreateDto> { new() { DrugId = 2, Quantity = 1, Dosage = "x" } }
        }, "u1");
        var id2 = await svc.CreateAsync(new PrescriptionCreateDto
        {
            CustomerId = customerId, DoctorName = "B", IssuedDate = DateTime.UtcNow,
            Items = new List<PrescriptionItemCreateDto> { new() { DrugId = 2, Quantity = 1, Dosage = "x" } }
        }, "u1");
        await svc.CancelAsync(id2);

        var issued = await svc.ListAsync(new PageRequest(), PrescriptionStatus.Issued);
        var cancelled = await svc.ListAsync(new PageRequest(), PrescriptionStatus.Cancelled);

        issued.TotalCount.Should().Be(1);
        cancelled.TotalCount.Should().Be(1);
        issued.Items.Single().Id.Should().Be(id1);
        cancelled.Items.Single().Id.Should().Be(id2);
    }
}
