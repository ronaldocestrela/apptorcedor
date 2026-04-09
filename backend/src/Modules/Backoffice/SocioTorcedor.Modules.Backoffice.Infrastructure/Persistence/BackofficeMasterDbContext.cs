using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Infrastructure.Persistence;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;

namespace SocioTorcedor.Modules.Backoffice.Infrastructure.Persistence;

public sealed class BackofficeMasterDbContext : BaseDbContext
{
    public BackofficeMasterDbContext(DbContextOptions<BackofficeMasterDbContext> options)
        : base(options)
    {
    }

    public DbSet<SaaSPlan> SaaSPlans => Set<SaaSPlan>();

    public DbSet<SaaSPlanFeature> SaaSPlanFeatures => Set<SaaSPlanFeature>();

    public DbSet<TenantPlan> TenantPlans => Set<TenantPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BackofficeMasterDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
