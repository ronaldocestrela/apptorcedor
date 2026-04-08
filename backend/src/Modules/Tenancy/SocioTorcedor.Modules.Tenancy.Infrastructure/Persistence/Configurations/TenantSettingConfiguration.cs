using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence.Configurations;

public sealed class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> builder)
    {
        builder.ToTable("TenantSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Key).HasMaxLength(256).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(4000).IsRequired();
        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.Settings)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
