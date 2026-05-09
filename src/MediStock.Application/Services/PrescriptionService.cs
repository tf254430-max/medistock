using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Domain.Enums;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public PrescriptionService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<PrescriptionListItemDto>> ListAsync(PageRequest request, PrescriptionStatus? status, CancellationToken ct = default)
    {
        IQueryable<Prescription> q = _db.Prescriptions.AsNoTracking();

        if (status is PrescriptionStatus s) q = q.Where(p => p.Status == s);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            q = q.Where(p => EF.Functions.Like(p.Customer.FullName, $"%{term}%")
                          || EF.Functions.Like(p.DoctorName, $"%{term}%"));
        }

        q = (request.Sort?.ToLowerInvariant(), request.Descending) switch
        {
            ("issued", false) => q.OrderBy(p => p.IssuedDate),
            ("issued", true) => q.OrderByDescending(p => p.IssuedDate),
            ("customer", false) => q.OrderBy(p => p.Customer.FullName),
            ("customer", true) => q.OrderByDescending(p => p.Customer.FullName),
            _ => q.OrderByDescending(p => p.IssuedDate)
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PrescriptionListItemDto
            {
                Id = p.Id,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer.FullName,
                DoctorName = p.DoctorName,
                IssuedDate = p.IssuedDate,
                Status = p.Status,
                LineCount = p.Items.Count,
                CreatedByName = p.CreatedBy.FullName
            })
            .ToListAsync(ct);

        return new PagedResult<PrescriptionListItemDto>
        {
            Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }

    public async Task<PrescriptionDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Prescriptions.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PrescriptionDetailDto
            {
                Id = p.Id,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer.FullName,
                DoctorName = p.DoctorName,
                IssuedDate = p.IssuedDate,
                Notes = p.Notes,
                Status = p.Status,
                CreatedByName = p.CreatedBy.FullName,
                CreatedAt = p.CreatedAt,
                Items = p.Items.Select(i => new PrescriptionItemDto
                {
                    Id = i.Id,
                    DrugId = i.DrugId,
                    DrugName = i.Drug.Name,
                    DrugStrength = i.Drug.Strength,
                    Quantity = i.Quantity,
                    Dosage = i.Dosage,
                    RequiresPrescription = i.Drug.RequiresPrescription,
                    CurrentStock = i.Drug.Batches
                        .Where(b => b.IsActive && b.ExpiryDate > now)
                        .Sum(b => b.QuantityRemaining)
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(PrescriptionCreateDto dto, string createdById, CancellationToken ct = default)
    {
        var entity = new Prescription
        {
            CustomerId = dto.CustomerId,
            DoctorName = dto.DoctorName.Trim(),
            IssuedDate = dto.IssuedDate.Date,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            Status = PrescriptionStatus.Issued,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
        foreach (var line in dto.Items.Where(l => l.Quantity > 0))
        {
            entity.Items.Add(new PrescriptionItem
            {
                DrugId = line.DrugId,
                Quantity = line.Quantity,
                Dosage = line.Dosage.Trim()
            });
        }
        _db.Prescriptions.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Prescription", entity.Id.ToString(), "Create", null, Snapshot(entity), ct);
        return entity.Id;
    }

    public async Task IssueAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.Prescriptions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException($"Prescription {id} not found.");
        if (p.Status != PrescriptionStatus.Draft) return;

        var before = Snapshot(p);
        p.Status = PrescriptionStatus.Issued;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Prescription", p.Id.ToString(), "Issue", before, Snapshot(p), ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.Prescriptions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException($"Prescription {id} not found.");
        if (p.Status == PrescriptionStatus.Dispensed) throw new InvalidOperationException("Cannot cancel a dispensed prescription.");
        if (p.Status == PrescriptionStatus.Cancelled) return;

        var before = Snapshot(p);
        p.Status = PrescriptionStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Prescription", p.Id.ToString(), "Cancel", before, Snapshot(p), ct);
    }

    public async Task MarkDispensedAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.Prescriptions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null || p.Status == PrescriptionStatus.Dispensed) return;

        var before = Snapshot(p);
        p.Status = PrescriptionStatus.Dispensed;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Prescription", p.Id.ToString(), "Dispense", before, Snapshot(p), ct);
    }

    private static object Snapshot(Prescription p) => new
    {
        p.Id, p.CustomerId, p.DoctorName, p.IssuedDate, p.Notes,
        Status = p.Status.ToString(), p.CreatedById, p.CreatedAt,
        Items = p.Items.Select(i => new { i.Id, i.DrugId, i.Quantity, i.Dosage }).ToArray()
    };
}
