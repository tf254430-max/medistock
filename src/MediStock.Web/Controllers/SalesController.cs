using System.Security.Claims;
using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Domain.Exceptions;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin,Cashier")]
public class SalesController : Controller
{
    private readonly ISaleService _sales;
    private readonly IReceiptPdfService _receiptPdf;
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;

    public SalesController(
        ISaleService sales,
        IReceiptPdfService receiptPdf,
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn)
    {
        _sales = sales;
        _receiptPdf = receiptPdf;
        _users = users;
        _signIn = signIn;
    }

    [HttpGet]
    public async Task<IActionResult> Till(CancellationToken ct)
    {
        var user = await _users.GetUserAsync(User);
        return View(new TillViewModel { CashierName = user?.FullName ?? "Cashier", Currency = "UGX" });
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (model.Lines.Count == 0)
            return Json(new { ok = false, error = "Cart is empty." });

        var dto = model.ToDto();
        var errors = await _sales.ValidateCartAsync(dto, ct);
        if (errors.Count > 0)
            return Json(new { ok = false, errors });

        try
        {
            var result = await _sales.CompleteSaleAsync(
                dto,
                User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                ct);

            return Json(new
            {
                ok = true,
                saleId = result.SaleId,
                receiptNumber = result.ReceiptNumber,
                receiptUrl = Url.Action(nameof(Receipt), new { id = result.SaleId })
            });
        }
        catch (InsufficientStockException ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int id, CancellationToken ct)
    {
        var sale = await _sales.GetByIdAsync(id, ct);
        if (sale is null) return NotFound();

        var path = _receiptPdf.PathFor(sale.ReceiptNumber);
        if (!System.IO.File.Exists(path)) return NotFound("Receipt file not found.");

        var stream = System.IO.File.OpenRead(path);
        return File(stream, "application/pdf", $"{sale.ReceiptNumber}.pdf");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? sort, bool desc = true, int page = 1, CancellationToken ct = default)
    {
        var req = new PageRequest { Page = page, Search = search, Sort = sort ?? "date", Descending = desc };
        var result = await _sales.ListAsync(req, ct);
        return View(new InventoryListViewModel<SaleListItemDto>
        {
            Page = result, Search = search, Sort = sort, Descending = desc
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var s = await _sales.GetByIdAsync(id, ct);
        if (s is null) return NotFound();
        return View(s);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Void(int id, CancellationToken ct)
    {
        var s = await _sales.GetByIdAsync(id, ct);
        if (s is null) return NotFound();
        if (s.IsVoided)
        {
            TempData["Error"] = "Sale is already voided.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if ((DateTime.UtcNow - s.CompletedAt).TotalMinutes > 30)
        {
            TempData["Error"] = "Sales can only be voided within 30 minutes of completion.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new VoidSaleViewModel { SaleId = s.Id, ReceiptNumber = s.ReceiptNumber, Reason = "" });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Void")]
    public async Task<IActionResult> VoidConfirmed(VoidSaleViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Void", vm);

        var admin = await _users.GetUserAsync(User);
        if (admin is null) return Forbid();

        var pwOk = await _users.CheckPasswordAsync(admin, vm.AdminPassword);
        if (!pwOk)
        {
            ModelState.AddModelError(nameof(vm.AdminPassword), "Password did not match.");
            return View("Void", vm);
        }

        try
        {
            await _sales.VoidSaleAsync(vm.SaleId, admin.Id, vm.Reason, ct);
            TempData["Success"] = $"Sale {vm.ReceiptNumber} voided. Stock restored.";
            return RedirectToAction(nameof(Details), new { id = vm.SaleId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = vm.SaleId });
        }
    }
}
