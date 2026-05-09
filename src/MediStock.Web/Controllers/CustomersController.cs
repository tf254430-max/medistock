using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin,Pharmacist,Cashier")]
public class CustomersController : Controller
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers) => _customers = customers;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, CancellationToken ct = default)
    {
        var req = new PageRequest { Page = page, Search = search, Sort = sort, Descending = desc };
        var result = await _customers.ListAsync(req, ct);
        return View(new InventoryListViewModel<CustomerListItemDto>
        {
            Page = result, Search = search, Sort = sort, Descending = desc
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var c = await _customers.GetByIdAsync(id, ct);
        if (c is null) return NotFound();
        return View(c);
    }

    [HttpGet]
    public IActionResult Create() => View(new CustomerCreateDto());

    [HttpPost]
    public async Task<IActionResult> Create(CustomerCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);
        var id = await _customers.CreateAsync(dto, ct);
        TempData["Success"] = $"Customer \"{dto.FullName}\" created.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var c = await _customers.GetByIdAsync(id, ct);
        if (c is null) return NotFound();
        return View(new CustomerUpdateDto
        {
            Id = c.Id, FullName = c.FullName, Phone = c.Phone, NIN = c.NIN, IsRecurring = c.IsRecurring
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CustomerUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);
        await _customers.UpdateAsync(dto, ct);
        TempData["Success"] = $"Customer \"{dto.FullName}\" updated.";
        return RedirectToAction(nameof(Details), new { id = dto.Id });
    }
}
