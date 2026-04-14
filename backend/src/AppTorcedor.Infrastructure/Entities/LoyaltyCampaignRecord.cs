using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class LoyaltyCampaignRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public LoyaltyCampaignStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? UnpublishedAt { get; set; }
}
