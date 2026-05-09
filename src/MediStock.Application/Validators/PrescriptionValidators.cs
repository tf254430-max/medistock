using FluentValidation;
using MediStock.Application.Dtos;

namespace MediStock.Application.Validators;

public class PrescriptionCreateDtoValidator : AbstractValidator<PrescriptionCreateDto>
{
    public PrescriptionCreateDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.IssuedDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one drug is required.");
        RuleForEach(x => x.Items).SetValidator(new PrescriptionItemCreateDtoValidator());
    }
}

public class PrescriptionItemCreateDtoValidator : AbstractValidator<PrescriptionItemCreateDto>
{
    public PrescriptionItemCreateDtoValidator()
    {
        RuleFor(x => x.DrugId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(250);
    }
}
