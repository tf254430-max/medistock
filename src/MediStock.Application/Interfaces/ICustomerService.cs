using MediStock.Application.Common;
using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> ListAllAsync(CancellationToken ct = default);
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CustomerCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(CustomerUpdateDto dto, CancellationToken ct = default);
}
