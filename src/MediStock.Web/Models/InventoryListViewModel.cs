using MediStock.Application.Common;

namespace MediStock.Web.Models;

public class InventoryListViewModel<T>
{
    public PagedResult<T> Page { get; set; } = new();
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public bool Descending { get; set; }
    public int? DrugIdFilter { get; set; }
}
