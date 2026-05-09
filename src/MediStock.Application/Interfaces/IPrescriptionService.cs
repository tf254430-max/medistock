using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Domain.Enums;

namespace MediStock.Application.Interfaces;

public interface IPrescriptionService
{
    Task<PagedResult<PrescriptionListItemDto>> ListAsync(PageRequest request, PrescriptionStatus? status, CancellationToken ct = default);
    Task<PrescriptionDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(PrescriptionCreateDto dto, string createdById, CancellationToken ct = default);
    Task IssueAsync(int id, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
    Task MarkDispensedAsync(int id, CancellationToken ct = default);
}
