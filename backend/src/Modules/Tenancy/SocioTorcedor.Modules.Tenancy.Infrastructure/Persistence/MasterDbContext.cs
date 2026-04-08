using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Infrastructure.Persistence;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence;

public sealed class MasterDbContext : BaseDbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();

    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
