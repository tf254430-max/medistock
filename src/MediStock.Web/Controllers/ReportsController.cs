using System.Globalization;
using System.Text;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly IReportingService _reports;

    public ReportsController(IReportingService reports) => _reports = reports;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var vm = new ReportsIndexViewModel
        {
            From = monthStart,
            To = monthEnd,
            DailyRevenue = await _reports.GetDailyRevenueAsync(30, ct),
            TopDrugs = await _reports.GetTopDrugsAsync(monthStart, monthEnd, 10, ct),
            ProfitMargin = (await _reports.GetProfitMarginAsync(monthStart, monthEnd, ct)).Take(20).ToList(),
            SupplierSpend = await _reports.GetSupplierSpendAsync(monthStart, monthEnd, ct),
            StockValuation = await _reports.GetStockValuationAsync(ct)
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Sales(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var f = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var t = to ?? f.AddMonths(1);
        var summary = await _reports.GetSalesReportAsync(f, t, ct);
        return View(summary);
    }

    [HttpGet]
    public async Task<IActionResult> SalesCsv(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var summary = await _reports.GetSalesReportAsync(from, to, ct);
        var sb = new StringBuilder();
        sb.AppendLine("ReceiptNumber,CompletedAt,Cashier,Customer,Payment,Items,SubTotal,Discount,VAT,Total,Status");
        foreach (var r in summary.Rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(r.ReceiptNumber),
                r.CompletedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                Csv(r.CashierName),
                Csv(r.CustomerName ?? "Walk-in"),
                Csv(r.PaymentMethod),
                r.Items.ToString(CultureInfo.InvariantCulture),
                r.SubTotal.ToString("0.##", CultureInfo.InvariantCulture),
                r.Discount.ToString("0.##", CultureInfo.InvariantCulture),
                r.VatAmount.ToString("0.##", CultureInfo.InvariantCulture),
                r.Total.ToString("0.##", CultureInfo.InvariantCulture),
                r.IsVoided ? "Voided" : "Completed"
            }));
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"sales-{from:yyyy-MM-dd}-to-{to:yyyy-MM-dd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> SalesPdf(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var summary = await _reports.GetSalesReportAsync(from, to, ct);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("MediStock — Sales Report").FontSize(16).Bold();
                    col.Item().AlignCenter().Text($"{from:yyyy-MM-dd} to {to:yyyy-MM-dd} (UTC)").FontSize(9);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Row(row =>
                    {
                        void Kpi(string label, string value)
                        {
                            row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(value).FontSize(13).SemiBold();
                            });
                        }
                        Kpi("Transactions", summary.TransactionCount.ToString("N0"));
                        Kpi("Subtotal (UGX)", summary.SubTotal.ToString("N0"));
                        Kpi("VAT (UGX)", summary.Vat.ToString("N0"));
                        Kpi("Total (UGX)", summary.Total.ToString("N0"));
                    });

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(1);
                        });

                        void H(string s) =>
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(s).SemiBold();
                        H("Receipt"); H("Date"); H("Cashier"); H("Customer"); H("Pay"); H("Total"); H("Status");

                        foreach (var r in summary.Rows)
                        {
                            table.Cell().Padding(4).Text(r.ReceiptNumber);
                            table.Cell().Padding(4).Text(r.CompletedAt.ToString("yyyy-MM-dd HH:mm"));
                            table.Cell().Padding(4).Text(r.CashierName);
                            table.Cell().Padding(4).Text(r.CustomerName ?? "Walk-in");
                            table.Cell().Padding(4).Text(r.PaymentMethod);
                            table.Cell().Padding(4).AlignRight().Text(r.Total.ToString("N0"));
                            table.Cell().Padding(4).Text(r.IsVoided ? "Voided" : "OK");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("MediStock report — generated ").FontSize(8);
                    t.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
                    t.Span(" UTC — page ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" of ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        });

        var bytes = doc.GeneratePdf();
        return File(bytes, "application/pdf", $"sales-{from:yyyy-MM-dd}-to-{to:yyyy-MM-dd}.pdf");
    }

    private static string Csv(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
}
