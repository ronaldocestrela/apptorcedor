using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Identity.Domain.Entities;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.RoleId).HasMaxLength(450).IsRequired();
        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
    }
}
