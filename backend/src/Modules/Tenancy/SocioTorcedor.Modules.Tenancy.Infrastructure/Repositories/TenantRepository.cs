using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;
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
}
