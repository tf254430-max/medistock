using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IUserAdminService _users;
    private readonly UserManager<AppUser> _userMgr;

    public UsersController(IUserAdminService users, UserManager<AppUser> userMgr)
    {
        _users = users;
        _userMgr = userMgr;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        var req = new PageRequest { Page = page, Search = search };
        var result = await _users.ListAsync(req, ct);
        return View(new InventoryListViewModel<UserListItemDto>
        {
            Page = result, Search = search
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct);
        if (u is null) return NotFound();
        return View(u);
    }

    [HttpGet]
    public IActionResult Create() => View(new UserCreateViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(UserCreateViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var (ok, errors) = await _users.CreateAsync(vm.ToDto(), ct);
        if (!ok)
        {
            foreach (var e in errors) ModelState.AddModelError("", e);
            return View(vm);
        }
        TempData["Success"] = $"User \"{vm.Email}\" created with role {vm.Role}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct);
        if (u is null) return NotFound();
        return View(new UserEditViewModel
        {
            Id = u.Id, Email = u.Email, FullName = u.FullName, Phone = u.Phone, Role = u.Role, IsActive = u.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UserEditViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var (ok, errors) = await _users.EditAsync(vm.ToDto(), ct);
        if (!ok)
        {
            foreach (var e in errors) ModelState.AddModelError("", e);
            return View(vm);
        }
        TempData["Success"] = "User updated.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct);
        if (u is null) return NotFound();
        return View(new UserResetPasswordViewModel { UserId = u.Id, Email = u.Email });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var admin = await _userMgr.GetUserAsync(User);
        if (admin is null) return Forbid();
        if (!await _userMgr.CheckPasswordAsync(admin, vm.AdminPassword))
        {
            ModelState.AddModelError(nameof(vm.AdminPassword), "Password did not match.");
            return View(vm);
        }

        var (ok, errors) = await _users.ResetPasswordAsync(vm.UserId, vm.NewPassword, ct);
        if (!ok)
        {
            foreach (var e in errors) ModelState.AddModelError("", e);
            return View(vm);
        }
        TempData["Success"] = $"Password reset for {vm.Email}.";
        return RedirectToAction(nameof(Details), new { id = vm.UserId });
    }

    [HttpGet]
    public async Task<IActionResult> Deactivate(string id, CancellationToken ct)
    {
        var u = await _users.GetByIdAsync(id, ct);
        if (u is null) return NotFound();
        if (!u.IsActive) return RedirectToAction(nameof(Details), new { id });
        return View(new UserDeactivateViewModel { UserId = u.Id, Email = u.Email });
    }

    [HttpPost, ActionName("Deactivate")]
    public async Task<IActionResult> DeactivateConfirmed(UserDeactivateViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Deactivate", vm);

        var admin = await _userMgr.GetUserAsync(User);
        if (admin is null) return Forbid();
        if (admin.Id == vm.UserId)
        {
            ModelState.AddModelError("", "You cannot deactivate your own account.");
            return View("Deactivate", vm);
        }
        if (!await _userMgr.CheckPasswordAsync(admin, vm.AdminPassword))
        {
            ModelState.AddModelError(nameof(vm.AdminPassword), "Password did not match.");
            return View("Deactivate", vm);
        }

        await _users.DeactivateAsync(vm.UserId, ct);
        TempData["Success"] = $"User {vm.Email} deactivated.";
        return RedirectToAction(nameof(Details), new { id = vm.UserId });
    }

    [HttpPost]
    public async Task<IActionResult> Reactivate(string id, CancellationToken ct)
    {
        await _users.ReactivateAsync(id, ct);
        TempData["Success"] = "User reactivated.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
