using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin,Pharmacist")]
public class SuppliersController : Controller
{
    private readonly ISupplierService _suppliers;

    public SuppliersController(ISupplierService suppliers) => _suppliers = suppliers;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var req = new PageRequest { Page = page, PageSize = pageSize, Search = search, Sort = sort, Descending = desc };
        var result = await _suppliers.ListAsync(req, ct);
        return View(new InventoryListViewModel<SupplierListItemDto>
        {
            Page = result, Search = search, Sort = sort, Descending = desc
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var s = await _suppliers.GetByIdAsync(id, ct);
        if (s is null) return NotFound();
        return View(s);
    }

    [HttpGet]
    public IActionResult Create() => View(new SupplierCreateDto());

    [HttpPost]
    public async Task<IActionResult> Create(SupplierCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);
        var id = await _suppliers.CreateAsync(dto, ct);
        TempData["Success"] = $"Supplier \"{dto.Name}\" created.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var s = await _suppliers.GetByIdAsync(id, ct);
        if (s is null) return NotFound();
        return View(new SupplierUpdateDto
        {
            Id = s.Id, Name = s.Name, ContactPerson = s.ContactPerson,
            Phone = s.Phone, Email = s.Email, Address = s.Address, Tin = s.Tin
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(SupplierUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);
        await _suppliers.UpdateAsync(dto, ct);
        TempData["Success"] = $"Supplier \"{dto.Name}\" updated.";
        return RedirectToAction(nameof(Details), new { id = dto.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _suppliers.DeactivateAsync(id, ct);
        TempData["Success"] = "Supplier deactivated.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
