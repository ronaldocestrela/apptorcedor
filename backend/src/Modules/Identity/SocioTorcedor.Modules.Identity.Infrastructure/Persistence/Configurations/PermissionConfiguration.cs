using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Identity.Domain.Entities;
namespace SocioTorcedor.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();
        builder.Property(p => p.Description).HasMaxLength(512).IsRequired();
        builder.Property(p => p.Type).HasConversion<int>();
    }
}
