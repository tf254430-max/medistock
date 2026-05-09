namespace MediStock.Application.Dtos;

public class DailyRevenuePoint
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int TransactionCount { get; set; }
}

public class TopDrugRow
{
    public int DrugId { get; set; }
    public string DrugName { get; set; } = null!;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class ProfitMarginRow
{
    public int DrugId { get; set; }
    public string DrugName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit => Revenue - Cost;
    public decimal MarginPct => Revenue == 0 ? 0 : (Profit / Revenue) * 100m;
}

public class SupplierSpendRow
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public int BatchCount { get; set; }
    public int UnitsReceived { get; set; }
    public decimal TotalSpend { get; set; }
}

public class StockValuationCategoryRow
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int UnitsOnHand { get; set; }
    public decimal Value { get; set; }
}

public class StockValuationDto
{
    public decimal TotalValue { get; set; }
    public int TotalUnits { get; set; }
    public List<StockValuationCategoryRow> ByCategory { get; set; } = new();
}

public class SalesReportRow
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public DateTime CompletedAt { get; set; }
    public string CashierName { get; set; } = null!;
    public string? CustomerName { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public int Items { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public bool IsVoided { get; set; }
}

public class SalesReportSummary
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TransactionCount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }
    public List<SalesReportRow> Rows { get; set; } = new();
}
