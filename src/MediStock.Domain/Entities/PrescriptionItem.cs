namespace MediStock.Domain.Entities;

public class PrescriptionItem
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public int DrugId { get; set; }
    public int Quantity { get; set; }
    public string Dosage { get; set; } = null!;

    public Prescription Prescription { get; set; } = null!;
    public Drug Drug { get; set; } = null!;
}
