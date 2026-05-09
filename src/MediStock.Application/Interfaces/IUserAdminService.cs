using MediStock.Application.Common;
using MediStock.Application.Dtos;

namespace MediStock.Application.Interfaces;

public interface IUserAdminService
{
    Task<PagedResult<UserListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default);
    Task<UserDetailDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<(bool ok, IReadOnlyList<string> errors)> CreateAsync(UserCreateDto dto, CancellationToken ct = default);
    Task<(bool ok, IReadOnlyList<string> errors)> EditAsync(UserEditDto dto, CancellationToken ct = default);
    Task<(bool ok, IReadOnlyList<string> errors)> ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default);
    Task DeactivateAsync(string userId, CancellationToken ct = default);
    Task ReactivateAsync(string userId, CancellationToken ct = default);
}
