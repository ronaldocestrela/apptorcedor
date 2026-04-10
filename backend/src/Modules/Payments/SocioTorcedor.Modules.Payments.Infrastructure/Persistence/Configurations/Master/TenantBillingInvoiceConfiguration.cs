using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence.Configurations.Master;

public sealed class TenantBillingInvoiceConfiguration : IEntityTypeConfiguration<TenantBillingInvoice>
{
    public void Configure(EntityTypeBuilder<TenantBillingInvoice> builder)
    {
        builder.ToTable("Payments_TenantBillingInvoices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8);
        builder.Property(x => x.ExternalInvoiceId).HasMaxLength(256);

        builder.HasIndex(x => x.TenantBillingSubscriptionId);
    }
}
