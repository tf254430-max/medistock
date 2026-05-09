namespace MediStock.Web.Models;

public class DashboardViewModel
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
