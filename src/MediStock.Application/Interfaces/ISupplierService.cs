using MediStock.Application.Common;
using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface ISupplierService
{
    Task<PagedResult<SupplierListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SupplierDto>> ListAllActiveAsync(CancellationToken ct = default);
    Task<SupplierDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(SupplierCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(SupplierUpdateDto dto, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}
