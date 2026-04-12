using Microsoft.EntityFrameworkCore;
using SocioTorcedor.BuildingBlocks.Infrastructure.Persistence;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Infrastructure.Persistence.Configurations.Master;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence;

public sealed class PaymentsMasterDbContext : BaseDbContext
{
    public PaymentsMasterDbContext(DbContextOptions<PaymentsMasterDbContext> options)
        : base(options)
    {
    }

    public DbSet<TenantBillingSubscription> TenantBillingSubscriptions => Set<TenantBillingSubscription>();

    public DbSet<TenantBillingInvoice> TenantBillingInvoices => Set<TenantBillingInvoice>();

    public DbSet<TenantPaymentWebhookInbox> TenantPaymentWebhookInbox => Set<TenantPaymentWebhookInbox>();

    public DbSet<MemberStripeWebhookInbox> MemberStripeWebhookInbox => Set<MemberStripeWebhookInbox>();

    public DbSet<TenantMemberGatewayConfiguration> TenantMemberGatewayConfigurations => Set<TenantMemberGatewayConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantBillingSubscriptionConfiguration());
        modelBuilder.ApplyConfiguration(new TenantBillingInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new TenantPaymentWebhookInboxConfiguration());
        modelBuilder.ApplyConfiguration(new MemberStripeWebhookInboxConfiguration());
        modelBuilder.ApplyConfiguration(new TenantMemberGatewayConfigurationConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
