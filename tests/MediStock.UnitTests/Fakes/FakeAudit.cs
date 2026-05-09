using MediStock.Application.Interfaces;

namespace MediStock.UnitTests.Fakes;

public class FakeAudit : IAuditService
{
    public List<(string EntityName, string EntityId, string Action)> Calls { get; } = new();
    public Task LogAsync(string entityName, string entityId, string action,
        object? oldValues, object? newValues, CancellationToken ct = default)
    {
        Calls.Add((entityName, entityId, action));
        return Task.CompletedTask;
    }
}
