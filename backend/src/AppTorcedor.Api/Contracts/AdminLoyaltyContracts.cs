using System.ComponentModel.DataAnnotations;
using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Api.Contracts;

public sealed class LoyaltyPointRuleRequestItem
{
    [Required]
    public LoyaltyPointRuleTrigger Trigger { get; set; }

    public int Points { get; set; }

    public int SortOrder { get; set; }
}

public sealed class UpsertLoyaltyCampaignRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = "";

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public List<LoyaltyPointRuleRequestItem> Rules { get; set; } = [];
}

public sealed class ManualLoyaltyPointsRequest
{
    public int Points { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = "";

    public Guid? CampaignId { get; set; }
}
