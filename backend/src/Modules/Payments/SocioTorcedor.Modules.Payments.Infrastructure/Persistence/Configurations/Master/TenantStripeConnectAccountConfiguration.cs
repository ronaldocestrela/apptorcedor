using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence.Configurations.Master;

public sealed class TenantStripeConnectAccountConfiguration : IEntityTypeConfiguration<TenantStripeConnectAccount>
{
    public void Configure(EntityTypeBuilder<TenantStripeConnectAccount> builder)
    {
        builder.ToTable("Payments_TenantStripeConnectAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StripeAccountId).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasIndex(x => x.StripeAccountId);
    }
}
