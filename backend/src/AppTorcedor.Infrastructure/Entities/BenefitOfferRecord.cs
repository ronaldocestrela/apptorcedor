namespace AppTorcedor.Infrastructure.Entities;

public sealed class BenefitOfferRecord
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
