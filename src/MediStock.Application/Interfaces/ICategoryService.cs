using MediStock.Application.Common;
using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface ICategoryService
{
    Task<PagedResult<CategoryListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDto>> ListAllAsync(CancellationToken ct = default);
    Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CategoryCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(CategoryUpdateDto dto, CancellationToken ct = default);
}
