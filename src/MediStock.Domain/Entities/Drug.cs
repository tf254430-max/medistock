using MediStock.Domain.Enums;

namespace MediStock.Domain.Entities;

public class Drug
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
    public bool IsActive { get; set; } = true;

    public Category Category { get; set; } = null!;
    public ICollection<Batch> Batches { get; set; } = new List<Batch>();

    public int CurrentStock => Batches
        .Where(b => b.IsActive && b.ExpiryDate > DateTime.UtcNow)
        .Sum(b => b.QuantityRemaining);
}
