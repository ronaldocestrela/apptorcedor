using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Infrastructure.Persistence;
using SocioTorcedor.Modules.Membership.Domain.Entities;

namespace SocioTorcedor.Modules.Membership.Infrastructure.Persistence;

public sealed class TenantMembershipDbContext : BaseDbContext
{
    public TenantMembershipDbContext(DbContextOptions<TenantMembershipDbContext> options)
        : base(options)
    {
    }

    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantMembershipDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
