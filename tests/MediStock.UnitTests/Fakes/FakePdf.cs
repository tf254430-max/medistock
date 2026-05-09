using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;

namespace MediStock.UnitTests.Fakes;

public class FakePdf : IReceiptPdfService
{
    public List<int> Generated { get; } = new();
    public Task<string> GenerateAsync(Sale sale, CancellationToken ct = default)
    {
        Generated.Add(sale.Id);
        return Task.FromResult($"data/receipts/{sale.ReceiptNumber}.pdf");
    }
    public string PathFor(string receiptNumber) => $"data/receipts/{receiptNumber}.pdf";
}
