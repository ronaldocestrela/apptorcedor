using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence.Configurations.Tenant;

public sealed class MemberBillingInvoiceConfiguration : IEntityTypeConfiguration<MemberBillingInvoice>
{
    public void Configure(EntityTypeBuilder<MemberBillingInvoice> builder)
    {
        builder.ToTable("Payments_MemberBillingInvoices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8);
        builder.Property(x => x.ExternalInvoiceId).HasMaxLength(256);
        builder.Property(x => x.PixCopyPaste).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.MemberBillingSubscriptionId);
    }
}
