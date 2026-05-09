namespace MediStock.Domain.Entities;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Tin { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}
