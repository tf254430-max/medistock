using MediStock.Domain.Enums;

namespace MediStock.Domain.Entities;

public class Prescription
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string DoctorName { get; set; } = null!;
    public DateTime IssuedDate { get; set; }
    public string? Notes { get; set; }
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
    public string CreatedById { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public AppUser CreatedBy { get; set; } = null!;
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
