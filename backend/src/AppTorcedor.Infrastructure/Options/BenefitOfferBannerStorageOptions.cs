namespace AppTorcedor.Infrastructure.Options;

public sealed class BenefitOfferBannerStorageOptions
{
    public const string SectionName = "BenefitOfferBanner";

    /// <summary>Storage provider: Local or Cloudinary.</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Absolute path root for stored files. Empty = {ContentRoot}/wwwroot/uploads/benefit-offer-banners.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>Max upload size in bytes (default 6 MB).</summary>
    public int MaxBytes { get; set; } = 6 * 1024 * 1024;

    public BenefitOfferBannerCloudinaryOptions Cloudinary { get; set; } = new();
}

public sealed class BenefitOfferBannerCloudinaryOptions
{
    public string Folder { get; set; } = "benefit-offer-banners";
}
