using System.Security.Claims;
using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Enums;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin,Pharmacist")]
public class PrescriptionsController : Controller
{
    private readonly IPrescriptionService _prescriptions;
    private readonly ICustomerService _customers;
    private readonly IDrugService _drugs;

    public PrescriptionsController(
        IPrescriptionService prescriptions,
        ICustomerService customers,
        IDrugService drugs)
    {
        _prescriptions = prescriptions;
        _customers = customers;
        _drugs = drugs;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? sort, bool desc = true, int page = 1, PrescriptionStatus? status = null, CancellationToken ct = default)
    {
        var req = new PageRequest { Page = page, Search = search, Sort = sort ?? "issued", Descending = desc };
        var result = await _prescriptions.ListAsync(req, status, ct);
        ViewBag.StatusFilter = status;
        return View(new InventoryListViewModel<PrescriptionListItemDto>
        {
            Page = result, Search = search, Sort = sort, Descending = desc
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var p = await _prescriptions.GetByIdAsync(id, ct);
        if (p is null) return NotFound();
        return View(p);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, CancellationToken ct)
    {
        var vm = new PrescriptionFormViewModel
        {
            Customers = await _customers.ListAllAsync(ct),
            Drugs = (await _drugs.ListAsync(new PageRequest { Page = 1, PageSize = 100 }, ct)).Items,
            CustomerId = customerId ?? 0,
            Items = new List<PrescriptionLineViewModel> { new() }
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PrescriptionFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm.Items.All(i => i.DrugId == 0))
        {
            if (vm.Items.All(i => i.DrugId == 0))
                ModelState.AddModelError("", "At least one prescription line is required.");
            vm.Customers = await _customers.ListAllAsync(ct);
            vm.Drugs = (await _drugs.ListAsync(new PageRequest { Page = 1, PageSize = 100 }, ct)).Items;
            return View(vm);
        }
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var id = await _prescriptions.CreateAsync(vm.ToDto(), userId, ct);
        TempData["Success"] = "Prescription created.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        try
        {
            await _prescriptions.CancelAsync(id, ct);
            TempData["Success"] = "Prescription cancelled.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Dispense(int id) =>
        RedirectToAction("Till", "Sales", new { prescriptionId = id });
}
