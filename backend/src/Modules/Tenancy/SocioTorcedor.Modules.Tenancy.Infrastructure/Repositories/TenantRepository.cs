using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Repositories;

public sealed class TenantRepository(MasterDbContext db) : ITenantRepository
{
    public async Task<TenantDto?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken)
    {
        var normalized = subdomain.Trim().ToLowerInvariant();
        var tenant = await db.Tenants
            .AsNoTracking()
            .Include(t => t.Domains)
            .FirstOrDefaultAsync(t => t.Subdomain == normalized, cancellationToken);

        if (tenant is null)
            return null;

        var origins = tenant.Domains.Select(d => d.Origin).ToList();
        return new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Subdomain,
            tenant.ConnectionString,
            origins);
    }
}
