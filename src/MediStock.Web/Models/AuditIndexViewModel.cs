using MediStock.Application.Common;
using MediStock.Application.Dtos;

namespace MediStock.Web.Models;

public class AuditIndexViewModel
{
    public PagedResult<AuditLogListItemDto> Page { get; set; } = new();
    public string? Search { get; set; }
    public string? EntityName { get; set; }
    public string? UserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public IReadOnlyList<string> EntityNames { get; set; } = Array.Empty<string>();
    public IReadOnlyList<UserListItemDto> Users { get; set; } = Array.Empty<UserListItemDto>();
}
