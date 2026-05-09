using MediStock.Application.Common;
using MediStock.Application.Dtos;
using MediStock.Application.Interfaces;
using MediStock.Domain.Entities;
using MediStock.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MediStock.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public CustomerService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<CustomerListItemDto>> ListAsync(PageRequest request, CancellationToken ct = default)
    {
        IQueryable<Customer> q = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(c => EF.Functions.Like(c.FullName, $"%{s}%")
                          || (c.Phone != null && EF.Functions.Like(c.Phone, $"%{s}%"))
                          || (c.NIN != null && EF.Functions.Like(c.NIN, $"%{s}%")));
        }

        q = (request.Sort?.ToLowerInvariant(), request.Descending) switch
        {
            ("name", true) => q.OrderByDescending(c => c.FullName),
            ("name", false) => q.OrderBy(c => c.FullName),
            ("created", true) => q.OrderByDescending(c => c.CreatedAt),
            ("created", false) => q.OrderBy(c => c.CreatedAt),
            _ => q.OrderBy(c => c.FullName)
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Phone = c.Phone,
                NIN = c.NIN,
                IsRecurring = c.IsRecurring,
                CreatedAt = c.CreatedAt,
                PrescriptionCount = c.Prescriptions.Count,
                SaleCount = c.Sales.Count
            })
            .ToListAsync(ct);

        return new PagedResult<CustomerListItemDto>
        {
            Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }

    public async Task<IReadOnlyList<CustomerDto>> ListAllAsync(CancellationToken ct = default)
    {
        return await _db.Customers.AsNoTracking()
            .OrderBy(c => c.FullName)
            .Select(c => new CustomerDto
            {
                Id = c.Id, FullName = c.FullName, Phone = c.Phone, NIN = c.NIN,
                IsRecurring = c.IsRecurring, CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Customers.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerDto
            {
                Id = c.Id, FullName = c.FullName, Phone = c.Phone, NIN = c.NIN,
                IsRecurring = c.IsRecurring, CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(CustomerCreateDto dto, CancellationToken ct = default)
    {
        var entity = new Customer
        {
            FullName = dto.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            NIN = string.IsNullOrWhiteSpace(dto.NIN) ? null : dto.NIN.Trim(),
            IsRecurring = dto.IsRecurring,
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Customer", entity.Id.ToString(), "Create", null, Snapshot(entity), ct);
        return entity.Id;
    }

    public async Task UpdateAsync(CustomerUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(c => c.Id == dto.Id, ct)
            ?? throw new KeyNotFoundException($"Customer {dto.Id} not found.");

        var before = Snapshot(entity);
        entity.FullName = dto.FullName.Trim();
        entity.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        entity.NIN = string.IsNullOrWhiteSpace(dto.NIN) ? null : dto.NIN.Trim();
        entity.IsRecurring = dto.IsRecurring;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("Customer", entity.Id.ToString(), "Update", before, Snapshot(entity), ct);
    }

    private static object Snapshot(Customer c) => new
    {
        c.Id, c.FullName, c.Phone, c.NIN, c.IsRecurring, c.CreatedAt
    };
}
