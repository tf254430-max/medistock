using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MediStock.Application.Services;

public class ReceiptPdfService : IReceiptPdfService
{
    private const string ReceiptsFolder = "data/receipts";

    private readonly AppDbContext _db;

    public ReceiptPdfService(AppDbContext db) => _db = db;

    public string PathFor(string receiptNumber) =>
        Path.Combine(ReceiptsFolder, $"{receiptNumber}.pdf");

    public async Task<string> GenerateAsync(Sale sale, CancellationToken ct = default)
    {
        Directory.CreateDirectory(ReceiptsFolder);

        var detail = await _db.Sales.AsNoTracking()
            .Where(s => s.Id == sale.Id)
            .Select(s => new ReceiptModel
            {
                ReceiptNumber = s.ReceiptNumber,
                CompletedAt = s.CompletedAt,
                CashierName = s.Cashier.FullName,
                CustomerName = s.Customer != null ? s.Customer.FullName : null,
                PaymentMethod = s.PaymentMethod,
                MobileMoneyRef = s.MobileMoneyRef,
                SubTotal = s.SubTotal,
                Discount = s.Discount,
                VatAmount = s.VatAmount,
                Total = s.Total,
                Lines = s.Items.Select(i => new ReceiptLine
                {
                    DrugName = i.Drug.Name,
                    BatchNumber = i.Batch.BatchNumber,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Sale {sale.Id} not found when generating receipt.");

        var settings = await _db.Settings.AsNoTracking()
            .ToDictionaryAsync(x => x.Key, x => x.Value ?? string.Empty, ct);

        detail.PharmacyName = settings.GetValueOrDefault("Pharmacy.Name", "MediStock Pharmacy");
        detail.PharmacyAddress = settings.GetValueOrDefault("Pharmacy.Address", string.Empty);
        detail.PharmacyPhone = settings.GetValueOrDefault("Pharmacy.Phone", string.Empty);
        detail.Currency = settings.GetValueOrDefault("Pharmacy.Currency", "UGX");

        var path = PathFor(detail.ReceiptNumber);
        await Task.Run(() => RenderDocument(detail).GeneratePdf(path), ct);
        return path;
    }

    private static IDocument RenderDocument(ReceiptModel m) => Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().AlignCenter().Text(m.PharmacyName).FontSize(18).Bold();
                if (!string.IsNullOrWhiteSpace(m.PharmacyAddress))
                    col.Item().AlignCenter().Text(m.PharmacyAddress).FontSize(9);
                if (!string.IsNullOrWhiteSpace(m.PharmacyPhone))
                    col.Item().AlignCenter().Text(m.PharmacyPhone).FontSize(9);
                col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(8);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Receipt #: ").SemiBold(); t.Span(m.ReceiptNumber); });
                        c.Item().Text(t => { t.Span("Date: ").SemiBold(); t.Span(m.CompletedAt.ToString("yyyy-MM-dd HH:mm") + " UTC"); });
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Cashier: ").SemiBold(); t.Span(m.CashierName); });
                        c.Item().Text(t => { t.Span("Customer: ").SemiBold(); t.Span(m.CustomerName ?? "Walk-in"); });
                    });
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(4);
                        cd.RelativeColumn(2);
                        cd.ConstantColumn(50);
                        cd.RelativeColumn(2);
                        cd.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Drug").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Batch").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Qty").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Unit").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Total").SemiBold();
                    });

                    foreach (var line in m.Lines)
                    {
                        table.Cell().Padding(6).Text(line.DrugName);
                        table.Cell().Padding(6).Text(line.BatchNumber);
                        table.Cell().Padding(6).AlignRight().Text(line.Quantity.ToString());
                        table.Cell().Padding(6).AlignRight().Text(line.UnitPrice.ToString("N0"));
                        table.Cell().Padding(6).AlignRight().Text(line.LineTotal.ToString("N0"));
                    }
                });

                col.Item().AlignRight().Width(220).Column(totals =>
                {
                    totals.Spacing(4);
                    void Row(string label, string value, bool bold = false)
                    {
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t => { var s = t.Span(label); if (bold) s.SemiBold(); });
                            r.ConstantItem(110).AlignRight().Text(t => { var s = t.Span($"{m.Currency} {value}"); if (bold) s.SemiBold(); });
                        });
                    }
                    Row("Subtotal", m.SubTotal.ToString("N0"));
                    Row("Discount", $"-{m.Discount.ToString("N0")}");
                    Row("VAT (18%)", m.VatAmount.ToString("N0"));
                    totals.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                    Row("TOTAL", m.Total.ToString("N0"), bold: true);
                });

                col.Item().PaddingTop(8).Text(t =>
                {
                    t.Span("Payment: ").SemiBold();
                    t.Span(m.PaymentMethod switch
                    {
                        PaymentMethod.Cash => "Cash",
                        PaymentMethod.MtnMoMo => "MTN Mobile Money",
                        PaymentMethod.AirtelMoney => "Airtel Money",
                        _ => m.PaymentMethod.ToString()
                    });
                    if (!string.IsNullOrWhiteSpace(m.MobileMoneyRef))
                    {
                        t.Span("   Ref: ").SemiBold();
                        t.Span(m.MobileMoneyRef);
                    }
                });
            });

            page.Footer().AlignCenter().Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                col.Item().PaddingTop(6).AlignCenter().Text("Thank you for shopping with us. Goods sold are not returnable except in case of defect.").FontSize(8).Italic();
                col.Item().AlignCenter().Text(t =>
                {
                    t.Span("Page ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" of ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        });
    });

    private class ReceiptModel
    {
        public string PharmacyName { get; set; } = "MediStock Pharmacy";
        public string PharmacyAddress { get; set; } = string.Empty;
        public string PharmacyPhone { get; set; } = string.Empty;
        public string Currency { get; set; } = "UGX";

        public string ReceiptNumber { get; set; } = null!;
        public DateTime CompletedAt { get; set; }
        public string CashierName { get; set; } = null!;
        public string? CustomerName { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? MobileMoneyRef { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }
        public List<ReceiptLine> Lines { get; set; } = new();
    }

    private class ReceiptLine
    {
        public string DrugName { get; set; } = null!;
        public string BatchNumber { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
