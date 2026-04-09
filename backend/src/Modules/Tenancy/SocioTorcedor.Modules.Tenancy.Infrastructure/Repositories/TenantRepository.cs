using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;
using SocioTorcedor.Modules.Tenancy.Domain.Enums;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Repositories;

public sealed class TenantRepository(MasterDbContext db) : ITenantRepository
{
    public async Task<TenantDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var tenant = await db.Tenants
            .AsNoTracking()
            .Include(t => t.Domains)
            .FirstOrDefaultAsync(t => t.Slug == normalized, cancellationToken);

        if (tenant is null)
            return null;

        var origins = tenant.Domains.Select(d => d.Origin).ToList();
        return new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.ConnectionString,
            origins);
    }

    public async Task<TenantDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .Include(t => t.Domains)
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant is null)
            return null;

        var domains = tenant.Domains
            .Select(d => new TenantDomainDto(d.Id, d.Origin))
            .ToList();

        var settings = tenant.Settings
            .Select(s => new TenantSettingDto(s.Id, s.Key, s.Value))
            .ToList();

        return new TenantDetailDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.ConnectionString,
            tenant.Status,
            tenant.CreatedAt,
            domains,
            settings);
    }

    public async Task<PagedResult<TenantListItemDto>> ListAsync(
        int page,
        int pageSize,
        string? search,
        TenantStatus? status,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Tenants.AsNoTracking();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var lower = term.ToLowerInvariant();
            query = query.Where(t =>
                t.Name.ToLower().Contains(lower) ||
                t.Slug.ToLower().Contains(lower));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TenantListItemDto(
                t.Id,
                t.Name,
                t.Slug,
                t.Status,
                t.CreatedAt,
                t.Domains.Count()))
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantListItemDto>(items, total, page, pageSize);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return await db.Tenants.AnyAsync(t => t.Slug == normalized, cancellationToken);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken) =>
        await db.Tenants.AddAsync(tenant, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<Tenant?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Tenants
            .Include(t => t.Domains)
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> GetTenantNamesByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<Guid, string>();

        var rows = await db.Tenants
            .AsNoTracking()
            .Where(t => idList.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Id, x => x.Name);
    }
}
