using MediStock.Application.Common;
using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface IDrugService
{
    Task<PagedResult<DrugListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default);
    Task<DrugDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(DrugCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(DrugUpdateDto dto, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}
