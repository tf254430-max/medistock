using FluentValidation;
using MediStock.Application.Dtos;

namespace MediStock.Application.Validators;

public class DrugCreateDtoValidator : AbstractValidator<DrugCreateDto>
{
    public DrugCreateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.GenericName).MaximumLength(150);
        RuleFor(x => x.NdaNumber).MaximumLength(40);
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Category is required.");
        RuleFor(x => x.Form).IsInEnum();
        RuleFor(x => x.Strength).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Barcode).MaximumLength(60);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}

public class DrugUpdateDtoValidator : AbstractValidator<DrugUpdateDto>
{
    public DrugUpdateDtoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.GenericName).MaximumLength(150);
        RuleFor(x => x.NdaNumber).MaximumLength(40);
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Category is required.");
        RuleFor(x => x.Form).IsInEnum();
        RuleFor(x => x.Strength).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Barcode).MaximumLength(60);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}
