using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Infrastructure.Persistence;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Infrastructure.Persistence.Configurations.Tenant;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence;

public sealed class TenantPaymentsDbContext : BaseDbContext
{
    public TenantPaymentsDbContext(DbContextOptions<TenantPaymentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<MemberBillingSubscription> MemberBillingSubscriptions => Set<MemberBillingSubscription>();

    public DbSet<MemberBillingInvoice> MemberBillingInvoices => Set<MemberBillingInvoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MemberBillingSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new MemberBillingInvoiceConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
