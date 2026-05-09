using System.ComponentModel.DataAnnotations;
using MediStock.Application.Dtos;

namespace MediStock.Web.Models;

public class UserCreateViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = null!;
    [Required, Display(Name = "Full name")] public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    [Required] public string Role { get; set; } = "Cashier";
    [Required, DataType(DataType.Password), MinLength(8)] public string Password { get; set; } = null!;

    public UserCreateDto ToDto() => new() { Email = Email, FullName = FullName, Phone = Phone, Role = Role, Password = Password };
}

public class UserEditViewModel
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    [Required, Display(Name = "Full name")] public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    [Required] public string Role { get; set; } = null!;
    public bool IsActive { get; set; }

    public UserEditDto ToDto() => new() { Id = Id, FullName = FullName, Phone = Phone, Role = Role };
}

public class UserResetPasswordViewModel
{
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;

    [Required, DataType(DataType.Password), MinLength(8)]
    [Display(Name = "New password for this user")]
    public string NewPassword { get; set; } = null!;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm your admin password")]
    public string AdminPassword { get; set; } = null!;
}

public class UserDeactivateViewModel
{
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm your admin password")]
    public string AdminPassword { get; set; } = null!;
}
