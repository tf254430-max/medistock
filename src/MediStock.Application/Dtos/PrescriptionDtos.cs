using MediStock.Domain.Enums;

namespace MediStock.Application.Dtos;

public class PrescriptionListItemDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = null!;
    public int CustomerId { get; set; }
    public string DoctorName { get; set; } = null!;
    public DateTime IssuedDate { get; set; }
    public PrescriptionStatus Status { get; set; }
    public int LineCount { get; set; }
    public string CreatedByName { get; set; } = null!;
}

public class PrescriptionDetailDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string DoctorName { get; set; } = null!;
    public DateTime IssuedDate { get; set; }
    public string? Notes { get; set; }
    public PrescriptionStatus Status { get; set; }
    public string CreatedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}

public class PrescriptionItemDto
{
    public int Id { get; set; }
    public int DrugId { get; set; }
    public string DrugName { get; set; } = null!;
    public string DrugStrength { get; set; } = null!;
    public int Quantity { get; set; }
    public string Dosage { get; set; } = null!;
    public bool RequiresPrescription { get; set; }
    public int CurrentStock { get; set; }
}

public class PrescriptionCreateDto
{
    public int CustomerId { get; set; }
    public string DoctorName { get; set; } = null!;
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow.Date;
    public string? Notes { get; set; }
    public List<PrescriptionItemCreateDto> Items { get; set; } = new();
}

public class PrescriptionItemCreateDto
{
    public int DrugId { get; set; }
    public int Quantity { get; set; }
    public string Dosage { get; set; } = null!;
}
