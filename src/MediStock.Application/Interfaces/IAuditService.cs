namespace MediStock.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? oldValues,
        object? newValues,
        CancellationToken ct = default);
}
