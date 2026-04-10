using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Persistence.Configurations.Tenant;

public sealed class MemberPaymentWebhookInboxConfiguration : IEntityTypeConfiguration<MemberPaymentWebhookInbox>
{
    public void Configure(EntityTypeBuilder<MemberPaymentWebhookInbox> builder)
    {
        builder.ToTable("Payments_MemberWebhookInbox");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdempotencyKey).HasMaxLength(256);
        builder.Property(x => x.EventType).HasMaxLength(128);
        builder.Property(x => x.RawPayload).HasColumnType("nvarchar(max)");
        builder.Property(x => x.LastError).HasMaxLength(2000);

        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
    }
}
