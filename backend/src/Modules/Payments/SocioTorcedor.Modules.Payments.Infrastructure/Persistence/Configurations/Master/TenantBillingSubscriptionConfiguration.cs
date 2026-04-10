using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence.Configurations.Master;

public sealed class TenantBillingSubscriptionConfiguration : IEntityTypeConfiguration<TenantBillingSubscription>
{
    public void Configure(EntityTypeBuilder<TenantBillingSubscription> builder)
    {
        builder.ToTable("Payments_TenantBillingSubscriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecurringAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8);
        builder.Property(x => x.ExternalCustomerId).HasMaxLength(256);
        builder.Property(x => x.ExternalSubscriptionId).HasMaxLength(256);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ExternalSubscriptionId);

        builder.HasMany(x => x.Invoices)
            .WithOne(x => x.Subscription)
            .HasForeignKey(x => x.TenantBillingSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
