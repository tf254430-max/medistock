using System.ComponentModel.DataAnnotations;
using MediStock.Application.Dtos;
using MediStock.Domain.Enums;

namespace MediStock.Web.Models;

public class DrugFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Brand name")]
    public string Name { get; set; } = null!;

    [Display(Name = "Generic name")]
    public string? GenericName { get; set; }

    [Display(Name = "NDA registration number")]
    public string? NdaNumber { get; set; }

    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Display(Name = "Dosage form")]
    public DosageForm Form { get; set; }

    public string Strength { get; set; } = null!;

    [Display(Name = "Requires prescription")]
    public bool RequiresPrescription { get; set; }

    public string? Barcode { get; set; }

    [Display(Name = "Reorder level")]
    public int ReorderLevel { get; set; } = 10;

    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();

    public DrugCreateDto ToCreateDto() => new()
    {
        Name = Name, GenericName = GenericName, NdaNumber = NdaNumber,
        CategoryId = CategoryId, Form = Form, Strength = Strength,
        RequiresPrescription = RequiresPrescription, Barcode = Barcode,
        ReorderLevel = ReorderLevel
    };

    public DrugUpdateDto ToUpdateDto() => new()
    {
        Id = Id, Name = Name, GenericName = GenericName, NdaNumber = NdaNumber,
        CategoryId = CategoryId, Form = Form, Strength = Strength,
        RequiresPrescription = RequiresPrescription, Barcode = Barcode,
        ReorderLevel = ReorderLevel
    };

    public static DrugFormViewModel FromDetail(DrugDetailDto d) => new()
    {
        Id = d.Id, Name = d.Name, GenericName = d.GenericName, NdaNumber = d.NdaNumber,
        CategoryId = d.CategoryId, Form = d.Form, Strength = d.Strength,
        RequiresPrescription = d.RequiresPrescription, Barcode = d.Barcode,
        ReorderLevel = d.ReorderLevel
    };
}
