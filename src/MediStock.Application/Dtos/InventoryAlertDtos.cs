namespace MediStock.Application.Dtos;

public class LowStockDrugDto
{
    public int DrugId { get; set; }
    public string DrugName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
}

public class ExpiringBatchDto
{
    public int BatchId { get; set; }
    public int DrugId { get; set; }
    public string DrugName { get; set; } = null!;
    public string BatchNumber { get; set; } = null!;
    public int QuantityRemaining { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class InventoryAlertSummary
{
    public int LowStockCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int Total => LowStockCount + ExpiringSoonCount;
}
