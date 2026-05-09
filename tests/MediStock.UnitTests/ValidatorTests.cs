using FluentAssertions;
using FluentValidation.TestHelper;
using MediStock.Application.Dtos;
using MediStock.Application.Validators;
using MediStock.Domain.Enums;

namespace MediStock.UnitTests;

public class ValidatorTests
{
    [Fact]
    public void DrugCreate_RequiresNameCategoryStrength()
    {
        var v = new DrugCreateDtoValidator();
        var r = v.TestValidate(new DrugCreateDto { Name = "", Strength = "", CategoryId = 0 });
        r.ShouldHaveValidationErrorFor(x => x.Name);
        r.ShouldHaveValidationErrorFor(x => x.Strength);
        r.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void BatchCreate_FuturePriceAndExpiry()
    {
        var v = new BatchCreateDtoValidator();
        var bad = v.TestValidate(new BatchCreateDto
        {
            DrugId = 0, BatchNumber = "", SupplierId = 0,
            Quantity = 0, CostPrice = -1, SellPrice = -2,
            ExpiryDate = DateTime.UtcNow.AddDays(-1)
        });
        bad.ShouldHaveValidationErrorFor(x => x.DrugId);
        bad.ShouldHaveValidationErrorFor(x => x.BatchNumber);
        bad.ShouldHaveValidationErrorFor(x => x.SupplierId);
        bad.ShouldHaveValidationErrorFor(x => x.Quantity);
        bad.ShouldHaveValidationErrorFor(x => x.ExpiryDate);

        var sellLowerThanCost = v.TestValidate(new BatchCreateDto
        {
            DrugId = 1, BatchNumber = "B", SupplierId = 1,
            Quantity = 1, CostPrice = 100, SellPrice = 50,
            ExpiryDate = DateTime.UtcNow.AddDays(60)
        });
        sellLowerThanCost.ShouldHaveValidationErrorFor(x => x.SellPrice);
    }

    [Fact]
    public void PrescriptionCreate_RequiresAtLeastOneItem()
    {
        var v = new PrescriptionCreateDtoValidator();
        var r = v.TestValidate(new PrescriptionCreateDto
        {
            CustomerId = 0,
            DoctorName = "",
            Items = new List<PrescriptionItemCreateDto>()
        });
        r.ShouldHaveValidationErrorFor(x => x.CustomerId);
        r.ShouldHaveValidationErrorFor(x => x.DoctorName);
        r.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void CategoryCreate_RequiresName()
    {
        var v = new CategoryCreateDtoValidator();
        v.TestValidate(new CategoryCreateDto { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);
        v.TestValidate(new CategoryCreateDto { Name = "OK" }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SupplierCreate_EmailValidatedWhenProvided()
    {
        var v = new SupplierCreateDtoValidator();
        v.TestValidate(new SupplierCreateDto { Name = "S", Email = "not-an-email" })
            .ShouldHaveValidationErrorFor(x => x.Email);
        v.TestValidate(new SupplierCreateDto { Name = "S" }).IsValid.Should().BeTrue();
        v.TestValidate(new SupplierCreateDto { Name = "S", Email = "x@y.ug" }).IsValid.Should().BeTrue();
    }
}
