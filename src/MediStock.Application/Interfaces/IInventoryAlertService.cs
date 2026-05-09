using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface IInventoryAlertService
{
    Task<IReadOnlyList<LowStockDrugDto>> GetLowStockAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ExpiringBatchDto>> GetExpiringSoonAsync(int withinDays = 30, CancellationToken ct = default);
    Task<InventoryAlertSummary> GetSummaryAsync(CancellationToken ct = default);
}
