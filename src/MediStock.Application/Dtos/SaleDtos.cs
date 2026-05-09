using MediStock.Domain.Enums;

namespace MediStock.Application.Dtos;

public class CheckoutDto
{
    public int? CustomerId { get; set; }
    public int? PrescriptionId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? MobileMoneyRef { get; set; }
    public decimal Discount { get; set; }
    public List<CheckoutLineDto> Lines { get; set; } = new();
}

public class CheckoutLineDto
{
    public int DrugId { get; set; }
    public int Quantity { get; set; }
}

public record SaleResult(int SaleId, string ReceiptNumber, string PdfPath);

public class CartValidationError
{
    public int DrugId { get; set; }
    public string DrugName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public int Requested { get; set; }
    public int Available { get; set; }
}

public class SaleListItemDto
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public DateTime CompletedAt { get; set; }
    public string CashierName { get; set; } = null!;
    public string? CustomerName { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Total { get; set; }
    public bool IsVoided { get; set; }
    public int LineCount { get; set; }
    public bool CanVoid => !IsVoided && (DateTime.UtcNow - CompletedAt).TotalMinutes <= 30;
}

public class SaleDetailDto
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public DateTime CompletedAt { get; set; }
    public string CashierName { get; set; } = null!;
    public string? CustomerName { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? MobileMoneyRef { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidedByName { get; set; }
    public string? VoidReason { get; set; }
    public List<SaleLineDto> Lines { get; set; } = new();
}

public class SaleLineDto
{
    public int DrugId { get; set; }
    public string DrugName { get; set; } = null!;
    public int BatchId { get; set; }
    public string BatchNumber { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class DrugSearchResultDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? GenericName { get; set; }
    public string Strength { get; set; } = null!;
    public bool RequiresPrescription { get; set; }
    public int CurrentStock { get; set; }
    public decimal? UnitPrice { get; set; }
}
