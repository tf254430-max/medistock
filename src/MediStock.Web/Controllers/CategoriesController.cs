using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin,Pharmacist")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories) => _categories = categories;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? sort, bool desc = false, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var req = new PageRequest { Page = page, PageSize = pageSize, Search = search, Sort = sort, Descending = desc };
        var result = await _categories.ListAsync(req, ct);
        return View(new InventoryListViewModel<CategoryListItemDto>
        {
            Page = result, Search = search, Sort = sort, Descending = desc
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var c = await _categories.GetByIdAsync(id, ct);
        if (c is null) return NotFound();
        return View(c);
    }

    [HttpGet]
    public IActionResult Create() => View(new CategoryCreateDto());

    [HttpPost]
    public async Task<IActionResult> Create(CategoryCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);
        var id = await _categories.CreateAsync(dto, ct);
        TempData["Success"] = $"Category \"{dto.Name}\" created.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var c = await _categories.GetByIdAsync(id, ct);
        if (c is null) return NotFound();
        return View(new CategoryUpdateDto { Id = c.Id, Name = c.Name, Description = c.Description });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CategoryUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);
        await _categories.UpdateAsync(dto, ct);
        TempData["Success"] = $"Category \"{dto.Name}\" updated.";
        return RedirectToAction(nameof(Details), new { id = dto.Id });
    }
}
