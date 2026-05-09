using MediStock.Application.Dtos;

namespace MediStock.Web.Models;

public class PrescriptionFormViewModel
{
    public int CustomerId { get; set; }
    public string DoctorName { get; set; } = null!;
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow.Date;
    public string? Notes { get; set; }
    public List<PrescriptionLineViewModel> Items { get; set; } = new();

    public IReadOnlyList<CustomerDto> Customers { get; set; } = Array.Empty<CustomerDto>();
    public IReadOnlyList<DrugListItemDto> Drugs { get; set; } = Array.Empty<DrugListItemDto>();

    public PrescriptionCreateDto ToDto() => new()
    {
        CustomerId = CustomerId,
        DoctorName = DoctorName,
        IssuedDate = IssuedDate,
        Notes = Notes,
        Items = Items.Where(i => i.DrugId > 0 && i.Quantity > 0)
            .Select(i => new PrescriptionItemCreateDto
            {
                DrugId = i.DrugId, Quantity = i.Quantity, Dosage = i.Dosage ?? string.Empty
            }).ToList()
    };
}

public class PrescriptionLineViewModel
{
    public int DrugId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Dosage { get; set; }
}
