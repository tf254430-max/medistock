namespace MediStock.Application.Interfaces;

public interface IReceiptNumberGenerator
{
    Task<string> NextAsync(CancellationToken ct = default);
}
