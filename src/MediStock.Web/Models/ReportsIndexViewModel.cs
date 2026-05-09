using MediStock.Application.Dtos;

namespace MediStock.Web.Models;

public class ReportsIndexViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public IReadOnlyList<DailyRevenuePoint> DailyRevenue { get; set; } = Array.Empty<DailyRevenuePoint>();
    public IReadOnlyList<TopDrugRow> TopDrugs { get; set; } = Array.Empty<TopDrugRow>();
    public IReadOnlyList<ProfitMarginRow> ProfitMargin { get; set; } = Array.Empty<ProfitMarginRow>();
    public IReadOnlyList<SupplierSpendRow> SupplierSpend { get; set; } = Array.Empty<SupplierSpendRow>();
    public StockValuationDto StockValuation { get; set; } = new();
}
