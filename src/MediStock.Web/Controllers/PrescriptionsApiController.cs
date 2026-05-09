using MediStock.Application.Interfaces;
using MediStock.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Authorize]
public class PrescriptionsApiController : ControllerBase
{
    private readonly IPrescriptionService _prescriptions;

    public PrescriptionsApiController(IPrescriptionService prescriptions) =>
        _prescriptions = prescriptions;

    [HttpGet("{id:int}/cart")]
    public async Task<IActionResult> Cart(int id, CancellationToken ct)
    {
        var p = await _prescriptions.GetByIdAsync(id, ct);
        if (p is null) return NotFound();

        if (p.Status == PrescriptionStatus.Cancelled)
            return BadRequest(new { error = "Prescription is cancelled." });
        if (p.Status == PrescriptionStatus.Dispensed)
            return BadRequest(new { error = "Prescription is already dispensed." });

        return Ok(new
        {
            prescriptionId = p.Id,
            customerId = p.CustomerId,
            customerName = p.CustomerName,
            doctorName = p.DoctorName,
            items = p.Items.Select(i => new
            {
                drugId = i.DrugId,
                name = i.DrugName,
                strength = i.DrugStrength,
                quantity = i.Quantity,
                dosage = i.Dosage,
                requiresPrescription = i.RequiresPrescription,
                currentStock = i.CurrentStock
            })
        });
    }
}
