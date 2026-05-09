using MediStock.Domain.Entities;

namespace MediStock.Application.Interfaces;

public interface IReceiptPdfService
{
    Task<string> GenerateAsync(Sale sale, CancellationToken ct = default);
    string PathFor(string receiptNumber);
}
