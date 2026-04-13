using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class LoyaltyPointLedgerEntryRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? RuleId { get; set; }
    public int Points { get; set; }
    public LoyaltyPointSourceType SourceType { get; set; }
    public string SourceKey { get; set; } = "";
    public string? Reason { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
