namespace AppTorcedor.Infrastructure.Entities;

public sealed class BenefitRedemptionRecord
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
