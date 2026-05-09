using MediStock.Application.Common;
using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface IAuditQueryService
{
    Task<PagedResult<AuditLogListItemDto>> ListAsync(
        PageRequest request,
        string? entityName,
        string? userId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> ListEntityNamesAsync(CancellationToken ct = default);
}
