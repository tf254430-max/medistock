using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class AuditQueryService : IAuditQueryService
{
    private readonly AppDbContext _db;
    public AuditQueryService(AppDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogListItemDto>> ListAsync(
        PageRequest request, string? entityName, string? userId,
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        IQueryable<AuditLog> q = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityName))
            q = q.Where(a => a.EntityName == entityName);
        if (!string.IsNullOrWhiteSpace(userId))
            q = q.Where(a => a.UserId == userId);
        if (from is DateTime f)
            q = q.Where(a => a.OccurredAt >= f);
        if (to is DateTime t)
            q = q.Where(a => a.OccurredAt < t);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(a => EF.Functions.Like(a.Action, $"%{s}%")
                          || EF.Functions.Like(a.EntityName, $"%{s}%")
                          || EF.Functions.Like(a.EntityId, $"%{s}%")
                          || (a.OldValuesJson != null && EF.Functions.Like(a.OldValuesJson, $"%{s}%"))
                          || (a.NewValuesJson != null && EF.Functions.Like(a.NewValuesJson, $"%{s}%")));
        }

        q = q.OrderByDescending(a => a.OccurredAt);

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogListItemDto
            {
                Id = a.Id,
                OccurredAt = a.OccurredAt,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Action = a.Action,
                UserId = a.UserId,
                UserDisplay = a.User != null ? a.User.FullName : null,
                IpAddress = a.IpAddress,
                OldValuesJson = a.OldValuesJson,
                NewValuesJson = a.NewValuesJson
            })
            .ToListAsync(ct);

        return new PagedResult<AuditLogListItemDto>
        {
            Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }

    public async Task<IReadOnlyList<string>> ListEntityNamesAsync(CancellationToken ct = default)
    {
        return await _db.AuditLogs.AsNoTracking()
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct);
    }
}
