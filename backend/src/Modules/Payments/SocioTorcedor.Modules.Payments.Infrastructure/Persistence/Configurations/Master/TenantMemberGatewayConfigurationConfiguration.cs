using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence.Configurations.Master;

public sealed class TenantMemberGatewayConfigurationConfiguration : IEntityTypeConfiguration<TenantMemberGatewayConfiguration>
{
    public void Configure(EntityTypeBuilder<TenantMemberGatewayConfiguration> builder)
    {
        builder.ToTable("Payments_TenantMemberGatewayConfigurations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProtectedCredentials).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}
