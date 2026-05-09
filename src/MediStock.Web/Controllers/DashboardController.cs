using MediStock.Application.Common;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<AppUser> _users;
    private readonly IReportingService _reports;
    private readonly IInventoryAlertService _alerts;
    private readonly ISaleService _sales;

    public DashboardController(
        UserManager<AppUser> users,
        IReportingService reports,
        IInventoryAlertService alerts,
        ISaleService sales)
    {
        _users = users;
        _reports = reports;
        _alerts = alerts;
        _sales = sales;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await _users.GetUserAsync(User);
        var roles = user is null ? Array.Empty<string>() : await _users.GetRolesAsync(user) as IList<string> ?? Array.Empty<string>();

        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        var todayReport = await _reports.GetSalesReportAsync(todayStart, todayEnd, ct);
        var alertSummary = await _alerts.GetSummaryAsync(ct);
        var dailyRevenue = await _reports.GetDailyRevenueAsync(30, ct);
        var topToday = await _reports.GetTopDrugsAsync(todayStart, todayEnd, 5, ct);
        var recent = await _sales.ListAsync(new PageRequest { Page = 1, PageSize = 10, Sort = "date", Descending = true }, ct);

        var vm = new DashboardViewModel
        {
            FullName = user?.FullName ?? User.Identity?.Name ?? "User",
            Email = user?.Email ?? string.Empty,
            Roles = roles.ToArray(),
            TodayRevenue = todayReport.Total,
            TodayTransactions = todayReport.TransactionCount,
            LowStockCount = alertSummary.LowStockCount,
            ExpiringSoonCount = alertSummary.ExpiringSoonCount,
            DailyRevenue = dailyRevenue,
            TopDrugsToday = topToday,
            RecentSales = recent.Items
        };
        return View(vm);
    }
}
