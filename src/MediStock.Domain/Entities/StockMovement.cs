using MediStock.Domain.Enums;

namespace MediStock.Domain.Entities;

public class StockMovement
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string? Reference { get; set; }
    public string PerformedById { get; set; } = null!;
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public Batch Batch { get; set; } = null!;
    public AppUser PerformedBy { get; set; } = null!;
}
