namespace MediStock.Application.Dtos;

public class AuditLogListItemDto
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? UserId { get; set; }
    public string? UserDisplay { get; set; }
    public string? IpAddress { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
}
