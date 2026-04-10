using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence.Configurations.Tenant;

public sealed class MemberBillingSubscriptionConfiguration : IEntityTypeConfiguration<MemberBillingSubscription>
{
    public void Configure(EntityTypeBuilder<MemberBillingSubscription> builder)
    {
        builder.ToTable("Payments_MemberBillingSubscriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecurringAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8);
        builder.Property(x => x.ExternalCustomerId).HasMaxLength(256);
        builder.Property(x => x.ExternalSubscriptionId).HasMaxLength(256);

        builder.HasIndex(x => x.MemberProfileId);
        builder.HasIndex(x => x.ExternalSubscriptionId);

        builder.HasMany(x => x.Invoices)
            .WithOne(x => x.Subscription)
            .HasForeignKey(x => x.MemberBillingSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
