using Microsoft.AspNetCore.Identity;

namespace MediStock.Domain.Entities;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
