using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface INotificationCache
{
    NotificationSnapshot Current { get; }
    Task RefreshAsync(CancellationToken ct = default);
}

public class NotificationSnapshot
{
    public DateTime LastRefreshedAt { get; set; }
    public IReadOnlyList<LowStockDrugDto> LowStock { get; set; } = Array.Empty<LowStockDrugDto>();
    public IReadOnlyList<ExpiringBatchDto> ExpiringSoon { get; set; } = Array.Empty<ExpiringBatchDto>();
    public int Total => LowStock.Count + ExpiringSoon.Count;
}
