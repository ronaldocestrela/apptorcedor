using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class LoyaltyPointRuleRecord
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public LoyaltyPointRuleTrigger Trigger { get; set; }
    public int Points { get; set; }
    public int SortOrder { get; set; }
}
