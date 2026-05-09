using System.ComponentModel.DataAnnotations;

namespace MediStock.Web.Models;

public class SettingsViewModel
{
    [Required, Display(Name = "Pharmacy name")]
    public string PharmacyName { get; set; } = "";

    [Display(Name = "Address")]
    public string? PharmacyAddress { get; set; }

    [Display(Name = "Phone")]
    public string? PharmacyPhone { get; set; }

    [Required, Display(Name = "Currency code"), MaxLength(8)]
    public string Currency { get; set; } = "UGX";

    [Required, Range(0, 1.0), Display(Name = "VAT rate (e.g. 0.18 for 18%)")]
    public decimal VatRate { get; set; } = 0.18m;
}
