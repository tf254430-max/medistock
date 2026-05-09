namespace MediStock.Application.Interfaces;

public interface ISettingsService
{
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default);
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(IDictionary<string, string?> values, CancellationToken ct = default);
}
