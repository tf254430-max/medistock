namespace MediStock.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }
}
