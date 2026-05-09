using MediStock.Application.Dtos;
using MediStock.Domain.Enums;

namespace MediStock.Web.Models;

public class CheckoutViewModel
{
    public int? CustomerId { get; set; }
    public int? PrescriptionId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? MobileMoneyRef { get; set; }
    public decimal Discount { get; set; }
    public List<CheckoutLineViewModel> Lines { get; set; } = new();

    public CheckoutDto ToDto() => new()
    {
        CustomerId = CustomerId,
        PrescriptionId = PrescriptionId,
        PaymentMethod = PaymentMethod,
        MobileMoneyRef = MobileMoneyRef,
        Discount = Discount,
        Lines = Lines.Select(l => new CheckoutLineDto { DrugId = l.DrugId, Quantity = l.Quantity }).ToList()
    };
}

public class CheckoutLineViewModel
{
    public int DrugId { get; set; }
    public int Quantity { get; set; }
}
