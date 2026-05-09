using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class DrugService : IDrugService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public DrugService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<DrugListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        IQueryable<Drug> q = _db.Drugs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(d => EF.Functions.Like(d.Name, $"%{s}%")
                          || (d.GenericName != null && EF.Functions.Like(d.GenericName, $"%{s}%"))
                          || (d.NdaNumber != null && EF.Functions.Like(d.NdaNumber, $"%{s}%"))
                          || (d.Barcode != null && EF.Functions.Like(d.Barcode, $"%{s}%")));
        }

        q = (request.Sort?.ToLowerInvariant(), request.Descending) switch
        {
            ("name", true) => q.OrderByDescending(d => d.Name),
            ("name", false) => q.OrderBy(d => d.Name),
            ("category", true) => q.OrderByDescending(d => d.Category.Name),
            ("category", false) => q.OrderBy(d => d.Category.Name),
            ("reorder", true) => q.OrderByDescending(d => d.ReorderLevel),
            ("reorder", false) => q.OrderBy(d => d.ReorderLevel),
            _ => q.OrderBy(d => d.Name)
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DrugListItemDto
            {
                Id = d.Id,
                Name = d.Name,
                GenericName = d.GenericName,
                CategoryName = d.Category.Name,
                Form = d.Form,
                Strength = d.Strength,
                RequiresPrescription = d.RequiresPrescription,
                ReorderLevel = d.ReorderLevel,
                IsActive = d.IsActive,
                CurrentStock = d.Batches
                    .Where(b => b.IsActive && b.ExpiryDate > now)
                    .Sum(b => b.QuantityRemaining)
            })
            .ToListAsync(ct);

        return new PagedResult<DrugListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<DrugDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Drugs.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DrugDetailDto
            {
                Id = d.Id,
                Name = d.Name,
                GenericName = d.GenericName,
                NdaNumber = d.NdaNumber,
                CategoryId = d.CategoryId,
                CategoryName = d.Category.Name,
                Form = d.Form,
                Strength = d.Strength,
                RequiresPrescription = d.RequiresPrescription,
                Barcode = d.Barcode,
                ReorderLevel = d.ReorderLevel,
                IsActive = d.IsActive,
                CurrentStock = d.Batches
                    .Where(b => b.IsActive && b.ExpiryDate > now)
                    .Sum(b => b.QuantityRemaining)
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(DrugCreateDto dto, CancellationToken ct = default)
    {
        var entity = new Drug
        {
            Name = dto.Name.Trim(),
            GenericName = Trim(dto.GenericName),
            NdaNumber = Trim(dto.NdaNumber),
            CategoryId = dto.CategoryId,
            Form = dto.Form,
            Strength = dto.Strength.Trim(),
            RequiresPrescription = dto.RequiresPrescription,
            Barcode = Trim(dto.Barcode),
            ReorderLevel = dto.ReorderLevel,
            IsActive = true
        };
        _db.Drugs.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Drug", entity.Id.ToString(), "Create", null, Snapshot(entity), ct);
        return entity.Id;
    }

    public async Task UpdateAsync(DrugUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Drugs.FirstOrDefaultAsync(d => d.Id == dto.Id, ct)
            ?? throw new KeyNotFoundException($"Drug {dto.Id} not found.");

        var before = Snapshot(entity);
        entity.Name = dto.Name.Trim();
        entity.GenericName = Trim(dto.GenericName);
        entity.NdaNumber = Trim(dto.NdaNumber);
        entity.CategoryId = dto.CategoryId;
        entity.Form = dto.Form;
        entity.Strength = dto.Strength.Trim();
        entity.RequiresPrescription = dto.RequiresPrescription;
        entity.Barcode = Trim(dto.Barcode);
        entity.ReorderLevel = dto.ReorderLevel;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Drug", entity.Id.ToString(), "Update", before, Snapshot(entity), ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Drugs.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new KeyNotFoundException($"Drug {id} not found.");

        if (!entity.IsActive) return;
        var before = Snapshot(entity);
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Drug", entity.Id.ToString(), "Deactivate", before, Snapshot(entity), ct);
    }

    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static object Snapshot(Drug d) => new
    {
        d.Id, d.Name, d.GenericName, d.NdaNumber, d.CategoryId,
        Form = d.Form.ToString(), d.Strength, d.RequiresPrescription,
        d.Barcode, d.ReorderLevel, d.IsActive
    };
}
