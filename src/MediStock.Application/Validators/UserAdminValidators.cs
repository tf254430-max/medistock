using FluentValidation;
using MediStock.Application.Dtos;

namespace MediStock.Application.Validators;

public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(120);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Role).NotEmpty().Must(r => new[] { "Admin", "Pharmacist", "Cashier" }.Contains(r))
            .WithMessage("Role must be one of Admin, Pharmacist, or Cashier.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
    }
}

public class UserEditDtoValidator : AbstractValidator<UserEditDto>
{
    public UserEditDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Role).NotEmpty().Must(r => new[] { "Admin", "Pharmacist", "Cashier" }.Contains(r))
            .WithMessage("Role must be one of Admin, Pharmacist, or Cashier.");
    }
}
