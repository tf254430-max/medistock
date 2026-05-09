using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public CategoryService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<CategoryListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default)
    {
        IQueryable<Category> q = _db.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(c => EF.Functions.Like(c.Name, $"%{s}%")
                          || (c.Description != null && EF.Functions.Like(c.Description, $"%{s}%")));
        }

        q = (request.Sort?.ToLowerInvariant(), request.Descending) switch
        {
            ("name", true) => q.OrderByDescending(c => c.Name),
            ("name", false) => q.OrderBy(c => c.Name),
            _ => q.OrderBy(c => c.Name)
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DrugCount = c.Drugs.Count
            })
            .ToListAsync(ct);

        return new PagedResult<CategoryListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<IReadOnlyList<CategoryDto>> ListAllAsync(CancellationToken ct = default)
    {
        return await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
            .ToListAsync(ct);
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(CategoryCreateDto dto, CancellationToken ct = default)
    {
        var entity = new Category
        {
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
        };
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("Category", entity.Id.ToString(), "Create", null,
            new { entity.Id, entity.Name, entity.Description }, ct);

        return entity.Id;
    }

    public async Task UpdateAsync(CategoryUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.Id, ct)
            ?? throw new KeyNotFoundException($"Category {dto.Id} not found.");

        var before = new { entity.Id, entity.Name, entity.Description };

        entity.Name = dto.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        await _db.SaveChangesAsync(ct);

        var after = new { entity.Id, entity.Name, entity.Description };
        await _audit.LogAsync("Category", entity.Id.ToString(), "Update", before, after, ct);
    }
}
