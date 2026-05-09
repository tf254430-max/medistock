using System.Text.Json;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace MediStock.Application.Services;

public class AuditService : IAuditService
{
    public const string UserIdItemKey = "Audit.UserId";
    public const string IpAddressItemKey = "Audit.IpAddress";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContext;

    public AuditService(AppDbContext db, IHttpContextAccessor httpContext)
    {
        _db = db;
        _httpContext = httpContext;
    }

    public async Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? oldValues,
        object? newValues,
        CancellationToken ct = default)
    {
        var ctx = _httpContext.HttpContext;
        string? userId = ctx?.Items[UserIdItemKey] as string;
        string? ip = ctx?.Items[IpAddressItemKey] as string;

        var log = new AuditLog
        {
            UserId = string.IsNullOrEmpty(userId) ? null : userId,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
            IpAddress = ip,
            OccurredAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
