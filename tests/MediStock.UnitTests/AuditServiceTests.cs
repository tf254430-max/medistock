using FluentAssertions;
using MediStock.Application.Services;
using MediStock.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MediStock.UnitTests;

public class AuditServiceTests
{
    private static IHttpContextAccessor BuildContext(string? userId, string? ip)
    {
        var ctx = new DefaultHttpContext();
        if (userId is not null) ctx.Items[AuditService.UserIdItemKey] = userId;
        if (ip is not null) ctx.Items[AuditService.IpAddressItemKey] = ip;
        return new Stub(ctx);
    }

    [Fact]
    public async Task Log_PersistsRow_WithUserAndIp()
    {
        using var ctx = TestDb.CreateInMemory();
        ctx.Users.Add(new AppUser { Id = "u1", UserName = "u1", FullName = "U", Email = "u@x", IsActive = true });
        await ctx.SaveChangesAsync();

        var svc = new AuditService(ctx, BuildContext("u1", "10.0.0.1"));
        await svc.LogAsync("Drug", "1", "Create", null, new { Name = "X" });

        var row = await ctx.AuditLogs.SingleAsync();
        row.UserId.Should().Be("u1");
        row.IpAddress.Should().Be("10.0.0.1");
        row.EntityName.Should().Be("Drug");
        row.Action.Should().Be("Create");
        row.NewValuesJson.Should().Contain("\"Name\":\"X\"");
        row.OldValuesJson.Should().BeNull();
    }

    [Fact]
    public async Task Log_WithoutContextUser_PersistsNullUserId()
    {
        using var ctx = TestDb.CreateInMemory();
        var svc = new AuditService(ctx, BuildContext(null, null));
        await svc.LogAsync("Test", "1", "Action", new { A = 1 }, new { A = 2 });

        var row = await ctx.AuditLogs.SingleAsync();
        row.UserId.Should().BeNull();
        row.IpAddress.Should().BeNull();
        row.OldValuesJson.Should().Contain("\"A\":1");
        row.NewValuesJson.Should().Contain("\"A\":2");
    }

    private class Stub : IHttpContextAccessor
    {
        public Stub(HttpContext ctx) { HttpContext = ctx; }
        public HttpContext? HttpContext { get; set; }
    }
}
