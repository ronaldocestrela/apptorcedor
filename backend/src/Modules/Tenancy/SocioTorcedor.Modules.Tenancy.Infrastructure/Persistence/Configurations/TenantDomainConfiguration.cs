using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Persistence.Configurations;

public sealed class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        builder.ToTable("TenantDomains");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Origin).HasMaxLength(512).IsRequired();
        builder.HasOne(d => d.Tenant)
            .WithMany(t => t.Domains)
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
