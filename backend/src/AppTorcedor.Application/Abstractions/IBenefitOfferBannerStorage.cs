namespace AppTorcedor.Application.Abstractions;

/// <summary>Stores benefit offer banner images; returns a public URL (relative /uploads/... or absolute Cloudinary URL).</summary>
public interface IBenefitOfferBannerStorage
{
    Task<string?> SaveBannerAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Best-effort delete of a previously stored banner (local file or Cloudinary).</summary>
    Task TryDeleteBannerAsync(string? bannerUrl, CancellationToken cancellationToken = default);
}
