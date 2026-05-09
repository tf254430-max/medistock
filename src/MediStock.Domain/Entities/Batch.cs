namespace MediStock.Domain.Entities;

public class Batch
{
    public int Id { get; set; }
    public int DrugId { get; set; }
    public string BatchNumber { get; set; } = null!;
    public int SupplierId { get; set; }
    public int QuantityIn { get; set; }
    public int QuantityRemaining { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellPrice { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public Drug Drug { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}
