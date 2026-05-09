using MediStock.Domain.Enums;

namespace MediStock.Domain.Entities;

public class Sale
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public int? CustomerId { get; set; }
    public int? PrescriptionId { get; set; }
    public string CashierId { get; set; } = null!;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? MobileMoneyRef { get; set; }
    public DateTime CompletedAt { get; set; }

    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidedById { get; set; }
    public string? VoidReason { get; set; }

    public Customer? Customer { get; set; }
    public Prescription? Prescription { get; set; }
    public AppUser Cashier { get; set; } = null!;
    public AppUser? VoidedBy { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
