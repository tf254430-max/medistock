using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class UserAdminService : IUserAdminService
{
    private readonly UserManager<AppUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public UserAdminService(
        UserManager<AppUser> users,
        RoleManager<IdentityRole> roles,
        AppDbContext db,
        IAuditService audit)
    {
        _users = users;
        _roles = roles;
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<UserListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default)
    {
        IQueryable<AppUser> q = _users.Users;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(u => EF.Functions.Like(u.Email!, $"%{s}%")
                          || EF.Functions.Like(u.FullName, $"%{s}%")
                          || (u.PhoneNumber != null && EF.Functions.Like(u.PhoneNumber, $"%{s}%")));
        }

        q = (request.Sort?.ToLowerInvariant(), request.Descending) switch
        {
            ("name", true) => q.OrderByDescending(u => u.FullName),
            ("name", false) => q.OrderBy(u => u.FullName),
            ("created", true) => q.OrderByDescending(u => u.CreatedAt),
            ("created", false) => q.OrderBy(u => u.CreatedAt),
            _ => q.OrderBy(u => u.FullName)
        };

        var total = await q.CountAsync(ct);
        var rows = await q
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new
            {
                u.Id, u.FullName, u.Email, u.PhoneNumber, u.IsActive, u.CreatedAt
            })
            .ToListAsync(ct);

        var items = new List<UserListItemDto>();
        foreach (var u in rows)
        {
            var roles = await _users.GetRolesAsync(new AppUser { Id = u.Id });
            items.Add(new UserListItemDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                Phone = u.PhoneNumber,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            });
        }

        return new PagedResult<UserListItemDto>
        {
            Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }

    public async Task<UserDetailDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var u = await _users.FindByIdAsync(id);
        if (u is null) return null;
        var roles = await _users.GetRolesAsync(u);
        return new UserDetailDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email!,
            Phone = u.PhoneNumber,
            Role = roles.FirstOrDefault() ?? string.Empty,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }

    public async Task<(bool ok, IReadOnlyList<string> errors)> CreateAsync(UserCreateDto dto, CancellationToken ct = default)
    {
        var role = dto.Role.Trim();
        if (!await _roles.RoleExistsAsync(role))
            return (false, new[] { $"Role '{role}' does not exist." });

        var existing = await _users.FindByEmailAsync(dto.Email);
        if (existing is not null)
            return (false, new[] { "A user with that email already exists." });

        var user = new AppUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            EmailConfirmed = true,
            FullName = dto.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var create = await _users.CreateAsync(user, dto.Password);
        if (!create.Succeeded)
            return (false, create.Errors.Select(e => e.Description).ToList());

        var addRole = await _users.AddToRoleAsync(user, role);
        if (!addRole.Succeeded)
        {
            await _users.DeleteAsync(user);
            return (false, addRole.Errors.Select(e => e.Description).ToList());
        }

        await _audit.LogAsync("User", user.Id, "Create", null,
            new { user.Id, user.Email, user.FullName, Role = role, user.IsActive }, ct);
        return (true, Array.Empty<string>());
    }

    public async Task<(bool ok, IReadOnlyList<string> errors)> EditAsync(UserEditDto dto, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(dto.Id);
        if (user is null) return (false, new[] { "User not found." });

        var oldRoles = await _users.GetRolesAsync(user);
        var oldSnapshot = new { user.Id, user.FullName, user.PhoneNumber, Role = oldRoles.FirstOrDefault() };

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
            return (false, update.Errors.Select(e => e.Description).ToList());

        var newRole = dto.Role.Trim();
        if (!oldRoles.Contains(newRole))
        {
            if (oldRoles.Count > 0)
                await _users.RemoveFromRolesAsync(user, oldRoles);
            if (!await _roles.RoleExistsAsync(newRole))
                return (false, new[] { $"Role '{newRole}' does not exist." });
            await _users.AddToRoleAsync(user, newRole);
        }

        await _audit.LogAsync("User", user.Id, "Update", oldSnapshot,
            new { user.Id, user.FullName, user.PhoneNumber, Role = newRole }, ct);
        return (true, Array.Empty<string>());
    }

    public async Task<(bool ok, IReadOnlyList<string> errors)> ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return (false, new[] { "User not found." });

        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description).ToList());

        await _audit.LogAsync("User", user.Id, "ResetPassword", null, new { user.Id, user.Email }, ct);
        return (true, Array.Empty<string>());
    }

    public async Task DeactivateAsync(string userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null || !user.IsActive) return;

        user.IsActive = false;
        await _users.UpdateAsync(user);
        await _users.UpdateSecurityStampAsync(user); // forces sign-out on next request
        await _audit.LogAsync("User", user.Id, "Deactivate", new { user.IsActive }, new { IsActive = false }, ct);
    }

    public async Task ReactivateAsync(string userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null || user.IsActive) return;

        user.IsActive = true;
        await _users.UpdateAsync(user);
        await _audit.LogAsync("User", user.Id, "Reactivate", new { IsActive = false }, new { IsActive = true }, ct);
    }
}
