using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface IReportingService
{
    Task<IReadOnlyList<DailyRevenuePoint>> GetDailyRevenueAsync(int days, CancellationToken ct = default);
    Task<IReadOnlyList<TopDrugRow>> GetTopDrugsAsync(DateTime from, DateTime to, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<ProfitMarginRow>> GetProfitMarginAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<SupplierSpendRow>> GetSupplierSpendAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<StockValuationDto> GetStockValuationAsync(CancellationToken ct = default);
    Task<SalesReportSummary> GetSalesReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
