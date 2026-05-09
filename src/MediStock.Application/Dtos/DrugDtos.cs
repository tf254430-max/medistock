using MediStock.Domain.Enums;

namespace MediStock.Application.Dtos;

public class DrugListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? GenericName { get; set; }
    public string CategoryName { get; set; } = null!;
    public DosageForm Form { get; set; }
    public string Strength { get; set; } = null!;
    public bool RequiresPrescription { get; set; }
    public int ReorderLevel { get; set; }
    public int CurrentStock { get; set; }
    public bool IsActive { get; set; }
}

public class DrugDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? GenericName { get; set; }
    public string? NdaNumber { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public DosageForm Form { get; set; }
    public string Strength { get; set; } = null!;
    public bool RequiresPrescription { get; set; }
    public string? Barcode { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; }
    public int CurrentStock { get; set; }
}

public class DrugCreateDto
{
    public string Name { get; set; } = null!;
    public string? GenericName { get; set; }
    public string? NdaNumber { get; set; }
    public int CategoryId { get; set; }
    public DosageForm Form { get; set; }
    public string Strength { get; set; } = null!;
    public bool RequiresPrescription { get; set; }
    public string? Barcode { get; set; }
    public int ReorderLevel { get; set; }
}

public class DrugUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? GenericName { get; set; }
    public string? NdaNumber { get; set; }
    public int CategoryId { get; set; }
    public DosageForm Form { get; set; }
    public string Strength { get; set; } = null!;
    public bool RequiresPrescription { get; set; }
    public string? Barcode { get; set; }
    public int ReorderLevel { get; set; }
}
