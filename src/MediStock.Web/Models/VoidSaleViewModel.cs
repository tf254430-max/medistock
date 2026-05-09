using System.ComponentModel.DataAnnotations;

namespace MediStock.Web.Models;

public class VoidSaleViewModel
{
    public int SaleId { get; set; }
    public string ReceiptNumber { get; set; } = null!;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm your admin password")]
    public string AdminPassword { get; set; } = null!;

    [Required, MaxLength(400)]
    public string Reason { get; set; } = null!;
}
