namespace MediStock.Domain.Entities;

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int DrugId { get; set; }
    public int BatchId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public Sale Sale { get; set; } = null!;
    public Drug Drug { get; set; } = null!;
    public Batch Batch { get; set; } = null!;
}
