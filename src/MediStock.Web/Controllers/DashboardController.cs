using MediStock.Domain.Entities;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<AppUser> _users;

    public DashboardController(UserManager<AppUser> users) => _users = users;

    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        var roles = user is null ? Array.Empty<string>() : await _users.GetRolesAsync(user) as IList<string> ?? Array.Empty<string>();

        return View(new DashboardViewModel
        {
            FullName = user?.FullName ?? User.Identity?.Name ?? "User",
            Email = user?.Email ?? string.Empty,
            Roles = roles.ToArray()
        });
    }
}
