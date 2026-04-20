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
    /// <summary>Public URL for hero/banner image (local /uploads/... or Cloudinary).</summary>
    public string? BannerUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
