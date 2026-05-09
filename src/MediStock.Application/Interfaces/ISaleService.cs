using MediStock.Application.Common;
using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface ISaleService
{
    Task<IReadOnlyList<CartValidationError>> ValidateCartAsync(CheckoutDto dto, CancellationToken ct = default);
    Task<SaleResult> CompleteSaleAsync(CheckoutDto dto, string cashierId, CancellationToken ct = default);
    Task VoidSaleAsync(int saleId, string adminUserId, string reason, CancellationToken ct = default);
    Task<PagedResult<SaleListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default);
    Task<SaleDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DrugSearchResultDto>> SearchDrugsAsync(string? q, CancellationToken ct = default);
}
