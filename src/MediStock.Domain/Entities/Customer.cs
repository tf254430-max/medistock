namespace MediStock.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? NIN { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
