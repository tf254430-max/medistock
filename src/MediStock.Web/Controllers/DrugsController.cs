using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin,Pharmacist")]
public class DrugsController : Controller
{
    private readonly IDrugService _drugs;
    private readonly ICategoryService _categories;
    private readonly IBatchService _batches;

    public DrugsController(IDrugService drugs, ICategoryService categories, IBatchService batches)
    {
        _drugs = drugs;
        _categories = categories;
        _batches = batches;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var req = new PageRequest { Page = page, PageSize = pageSize, Search = search, Sort = sort, Descending = desc };
        var result = await _drugs.ListAsync(req, ct);
        return View(new InventoryListViewModel<DrugListItemDto>
        {
            Page = result, Search = search, Sort = sort, Descending = desc
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var d = await _drugs.GetByIdAsync(id, ct);
        if (d is null) return NotFound();
        ViewBag.Batches = await _batches.ListForDrugAsync(id, ct);
        return View(d);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var vm = new DrugFormViewModel { Categories = await _categories.ListAllAsync(ct) };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(DrugFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _categories.ListAllAsync(ct);
            return View(vm);
        }
        var id = await _drugs.CreateAsync(vm.ToCreateDto(), ct);
        TempData["Success"] = $"Drug \"{vm.Name}\" created.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var d = await _drugs.GetByIdAsync(id, ct);
        if (d is null) return NotFound();
        var vm = DrugFormViewModel.FromDetail(d);
        vm.Categories = await _categories.ListAllAsync(ct);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(DrugFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _categories.ListAllAsync(ct);
            return View(vm);
        }
        await _drugs.UpdateAsync(vm.ToUpdateDto(), ct);
        TempData["Success"] = $"Drug \"{vm.Name}\" updated.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _drugs.DeactivateAsync(id, ct);
        TempData["Success"] = "Drug deactivated.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
