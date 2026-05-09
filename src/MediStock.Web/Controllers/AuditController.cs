using MediStock.Application.Common;
using MediStock.Application.Interfaces;
using MediStock.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediStock.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AuditController : Controller
{
    private readonly IAuditQueryService _audit;
    private readonly IUserAdminService _users;

    public AuditController(IAuditQueryService audit, IUserAdminService users)
    {
        _audit = audit;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, string? entityName, string? userId,
        DateTime? from, DateTime? to, int page = 1,
        CancellationToken ct = default)
    {
        var req = new PageRequest { Page = page, PageSize = 30, Search = search };
        var pageResult = await _audit.ListAsync(req, entityName, userId, from, to, ct);
        var entityNames = await _audit.ListEntityNamesAsync(ct);
        var users = await _users.ListAsync(new PageRequest { Page = 1, PageSize = 100 }, ct);

        return View(new AuditIndexViewModel
        {
            Page = pageResult,
            Search = search,
            EntityName = entityName,
            UserId = userId,
            From = from,
            To = to,
            EntityNames = entityNames,
            Users = users.Items
        });
    }
}
