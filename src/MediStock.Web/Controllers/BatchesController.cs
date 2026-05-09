using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin,Pharmacist")]
public class BatchesController : Controller
{
    private readonly IBatchService _batches;
    private readonly IDrugService _drugs;
    private readonly ISupplierService _suppliers;

    public BatchesController(IBatchService batches, IDrugService drugs, ISupplierService suppliers)
    {
        _batches = batches;
        _drugs = drugs;
        _suppliers = suppliers;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = 20, int? drugId = null, CancellationToken ct = default)
    {
        var req = new PageRequest { Page = page, PageSize = pageSize, Search = search, Sort = sort, Descending = desc };
        var result = await _batches.ListAsync(req, drugId, ct);
        return View(new InventoryListViewModel<BatchListItemDto>
        {
            Page = result, Search = search, Sort = sort, Descending = desc, DrugIdFilter = drugId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var b = await _batches.GetByIdAsync(id, ct);
        if (b is null) return NotFound();
        return View(b);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? drugId, CancellationToken ct)
    {
        var drugs = await _drugs.ListAsync(new PageRequest { Page = 1, PageSize = 100 }, ct);
        var vm = new BatchCreateViewModel
        {
            DrugId = drugId ?? 0,
            Drugs = drugs.Items,
            Suppliers = await _suppliers.ListAllActiveAsync(ct)
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BatchCreateViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var drugs = await _drugs.ListAsync(new PageRequest { Page = 1, PageSize = 100 }, ct);
            vm.Drugs = drugs.Items;
            vm.Suppliers = await _suppliers.ListAllActiveAsync(ct);
            return View(vm);
        }
        var id = await _batches.CreateAsync(vm.ToDto(), ct);
        TempData["Success"] = $"Batch \"{vm.BatchNumber}\" added.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
