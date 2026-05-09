using MediStock.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[ApiController]
[Route("api/drugs")]
[Authorize]
public class DrugsApiController : ControllerBase
{
    private readonly ISaleService _sales;

    public DrugsApiController(ISaleService sales) => _sales = sales;

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken ct)
    {
        var results = await _sales.SearchDrugsAsync(q, ct);
        return Ok(results);
    }
}
