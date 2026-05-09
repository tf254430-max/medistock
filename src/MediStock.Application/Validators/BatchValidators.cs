using FluentValidation;
using MediStock.Application.Dtos;

namespace MediStock.Application.Validators;

public class BatchCreateDtoValidator : AbstractValidator<BatchCreateDto>
{
    public BatchCreateDtoValidator()
    {
        RuleFor(x => x.DrugId).GreaterThan(0).WithMessage("Drug is required.");
        RuleFor(x => x.BatchNumber).NotEmpty().MaximumLength(60);
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("Supplier is required.");
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellPrice)
            .GreaterThanOrEqualTo(0)
            .GreaterThanOrEqualTo(x => x.CostPrice)
            .WithMessage("Sell price must not be lower than cost price.");
        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.UtcNow.Date)
            .WithMessage("Expiry date must be in the future.");
    }
}
