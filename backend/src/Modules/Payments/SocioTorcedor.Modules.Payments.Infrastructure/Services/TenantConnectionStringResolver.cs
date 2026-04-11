using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

public sealed class TenantConnectionStringResolver(MasterDbContext db) : ITenantConnectionStringResolver
{
    public async Task<string?> GetConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        return tenant?.ConnectionString;
    }
}
