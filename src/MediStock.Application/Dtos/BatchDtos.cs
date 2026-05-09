namespace MediStock.Application.Dtos;

public class BatchListItemDto
{
    public int Id { get; set; }
    public int DrugId { get; set; }
    public string DrugName { get; set; } = null!;
    public string BatchNumber { get; set; } = null!;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public int QuantityIn { get; set; }
    public int QuantityRemaining { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellPrice { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime ReceivedAt { get; set; }
    public bool IsActive { get; set; }

    public int DaysUntilExpiry => (int)(ExpiryDate.Date - DateTime.UtcNow.Date).TotalDays;
    public bool IsExpired => ExpiryDate <= DateTime.UtcNow;
}

public class BatchCreateDto
{
    public int DrugId { get; set; }
    public string BatchNumber { get; set; } = null!;
    public int SupplierId { get; set; }
    public int Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellPrice { get; set; }
    public DateTime ExpiryDate { get; set; }
}
