using MediStock.Application.Common;
using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface IBatchService
{
    Task<PagedResult<BatchListItemDto>> ListAsync(PageRequest request, int? drugId, CancellationToken ct = default);
    Task<BatchListItemDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<BatchListItemDto>> ListForDrugAsync(int drugId, CancellationToken ct = default);
    Task<int> CreateAsync(BatchCreateDto dto, CancellationToken ct = default);
    Task<int> MarkExpiredAsync(CancellationToken ct = default);
}
