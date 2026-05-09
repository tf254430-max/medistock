namespace MediStock.Application.Dtos;

public class CustomerListItemDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? NIN { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PrescriptionCount { get; set; }
    public int SaleCount { get; set; }
}

public class CustomerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? NIN { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerCreateDto
{
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? NIN { get; set; }
    public bool IsRecurring { get; set; }
}

public class CustomerUpdateDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? NIN { get; set; }
    public bool IsRecurring { get; set; }
}
