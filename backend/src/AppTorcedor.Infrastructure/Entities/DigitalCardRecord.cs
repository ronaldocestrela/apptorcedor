using AppTorcedor.Identity;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class DigitalCardRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MembershipId { get; set; }
    public int Version { get; set; }
    public DigitalCardStatus Status { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }
    public string? InvalidationReason { get; set; }
}
