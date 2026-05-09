using System.Globalization;
using MediStock.Application.Interfaces;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly ISettingsService _settings;

    public SettingsController(ISettingsService settings) => _settings = settings;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var s = await _settings.GetAllAsync(ct);
        var vm = new SettingsViewModel
        {
            PharmacyName = s.GetValueOrDefault("Pharmacy.Name", "MediStock Pharmacy"),
            PharmacyAddress = s.GetValueOrDefault("Pharmacy.Address", string.Empty),
            PharmacyPhone = s.GetValueOrDefault("Pharmacy.Phone", string.Empty),
            Currency = s.GetValueOrDefault("Pharmacy.Currency", "UGX"),
            VatRate = decimal.TryParse(s.GetValueOrDefault("Tax.VatRate", "0.18"),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0.18m
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Index(SettingsViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        await _settings.SetAsync(new Dictionary<string, string?>
        {
            ["Pharmacy.Name"] = vm.PharmacyName.Trim(),
            ["Pharmacy.Address"] = string.IsNullOrWhiteSpace(vm.PharmacyAddress) ? null : vm.PharmacyAddress.Trim(),
            ["Pharmacy.Phone"] = string.IsNullOrWhiteSpace(vm.PharmacyPhone) ? null : vm.PharmacyPhone.Trim(),
            ["Pharmacy.Currency"] = vm.Currency.Trim().ToUpperInvariant(),
            ["Tax.VatRate"] = vm.VatRate.ToString("0.####", CultureInfo.InvariantCulture)
        }, ct);

        TempData["Success"] = "Settings saved. New VAT rate applies to future sales only — past sales are not recalculated.";
        return RedirectToAction(nameof(Index));
    }
}
