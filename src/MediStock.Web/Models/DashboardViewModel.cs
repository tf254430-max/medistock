using MediStock.Application.Dtos;

namespace MediStock.Web.Models;

public class DashboardViewModel
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    // KPIs
    public decimal TodayRevenue { get; set; }
    public int TodayTransactions { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringSoonCount { get; set; }

    public IReadOnlyList<DailyRevenuePoint> DailyRevenue { get; set; } = Array.Empty<DailyRevenuePoint>();
    public IReadOnlyList<TopDrugRow> TopDrugsToday { get; set; } = Array.Empty<TopDrugRow>();
    public IReadOnlyList<SaleListItemDto> RecentSales { get; set; } = Array.Empty<SaleListItemDto>();

    public bool IsAdmin => Roles.Contains("Admin");
    public bool IsPharmacist => Roles.Contains("Pharmacist");
    public bool IsCashier => Roles.Contains("Cashier");
}
