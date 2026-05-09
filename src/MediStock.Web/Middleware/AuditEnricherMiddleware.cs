using System.Security.Claims;
using MediStock.Application.Services;

namespace MediStock.Web.Middleware;

public class AuditEnricherMiddleware
{
    private readonly RequestDelegate _next;

    public AuditEnricherMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var userId = ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
            ctx.Items[AuditService.UserIdItemKey] = userId;

        var ip = ctx.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ip))
            ctx.Items[AuditService.IpAddressItemKey] = ip;

        await _next(ctx);
    }
}
