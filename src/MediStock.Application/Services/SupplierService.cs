using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public SupplierService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<SupplierListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default)
    {
        IQueryable<Supplier> q = _db.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(x => EF.Functions.Like(x.Name, $"%{s}%")
                          || (x.ContactPerson != null && EF.Functions.Like(x.ContactPerson, $"%{s}%"))
                          || (x.Phone != null && EF.Functions.Like(x.Phone, $"%{s}%"))
                          || (x.Email != null && EF.Functions.Like(x.Email, $"%{s}%")));
        }

        q = (request.Sort?.ToLowerInvariant(), request.Descending) switch
        {
            ("name", true) => q.OrderByDescending(x => x.Name),
            ("name", false) => q.OrderBy(x => x.Name),
            ("contact", true) => q.OrderByDescending(x => x.ContactPerson),
            ("contact", false) => q.OrderBy(x => x.ContactPerson),
            _ => q.OrderBy(x => x.Name)
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SupplierListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                ContactPerson = x.ContactPerson,
                Phone = x.Phone,
                IsActive = x.IsActive,
                BatchCount = x.Batches.Count
            })
            .ToListAsync(ct);

        return new PagedResult<SupplierListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<IReadOnlyList<SupplierDto>> ListAllActiveAsync(CancellationToken ct = default)
    {
        return await _db.Suppliers.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                Tin = s.Tin,
                IsActive = s.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<SupplierDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Suppliers.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                Tin = s.Tin,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(SupplierCreateDto dto, CancellationToken ct = default)
    {
        var entity = new Supplier
        {
            Name = dto.Name.Trim(),
            ContactPerson = Trim(dto.ContactPerson),
            Phone = Trim(dto.Phone),
            Email = Trim(dto.Email),
            Address = Trim(dto.Address),
            Tin = Trim(dto.Tin),
            IsActive = true
        };
        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Supplier", entity.Id.ToString(), "Create", null, Snapshot(entity), ct);
        return entity.Id;
    }

    public async Task UpdateAsync(SupplierUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == dto.Id, ct)
            ?? throw new KeyNotFoundException($"Supplier {dto.Id} not found.");

        var before = Snapshot(entity);
        entity.Name = dto.Name.Trim();
        entity.ContactPerson = Trim(dto.ContactPerson);
        entity.Phone = Trim(dto.Phone);
        entity.Email = Trim(dto.Email);
        entity.Address = Trim(dto.Address);
        entity.Tin = Trim(dto.Tin);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Supplier", entity.Id.ToString(), "Update", before, Snapshot(entity), ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException($"Supplier {id} not found.");

        if (!entity.IsActive) return;
        var before = Snapshot(entity);
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Supplier", entity.Id.ToString(), "Deactivate", before, Snapshot(entity), ct);
    }

    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static object Snapshot(Supplier s) => new
    {
        s.Id, s.Name, s.ContactPerson, s.Phone, s.Email, s.Address, s.Tin, s.IsActive
    };
}
