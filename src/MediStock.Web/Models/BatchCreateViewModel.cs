using System.ComponentModel.DataAnnotations;
using MediStock.Application.Dtos;

namespace MediStock.Web.Models;

public class BatchCreateViewModel
{
    [Display(Name = "Drug")]
    public int DrugId { get; set; }

    [Display(Name = "Batch number")]
    public string BatchNumber { get; set; } = null!;

    [Display(Name = "Supplier")]
    public int SupplierId { get; set; }

    public int Quantity { get; set; }

    [Display(Name = "Cost price (UGX)")]
    public decimal CostPrice { get; set; }

    [Display(Name = "Sell price (UGX)")]
    public decimal SellPrice { get; set; }

    [Display(Name = "Expiry date")]
    [DataType(DataType.Date)]
    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMonths(12);

    public IReadOnlyList<DrugListItemDto> Drugs { get; set; } = Array.Empty<DrugListItemDto>();
    public IReadOnlyList<SupplierDto> Suppliers { get; set; } = Array.Empty<SupplierDto>();

    public BatchCreateDto ToDto() => new()
    {
        DrugId = DrugId,
        BatchNumber = BatchNumber,
        SupplierId = SupplierId,
        Quantity = Quantity,
        CostPrice = CostPrice,
        SellPrice = SellPrice,
        ExpiryDate = ExpiryDate
    };
}
