namespace MediStock.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<Drug> Drugs { get; set; } = new List<Drug>();
}
