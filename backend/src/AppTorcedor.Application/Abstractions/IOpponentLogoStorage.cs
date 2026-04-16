namespace AppTorcedor.Application.Abstractions;

/// <summary>Stores opponent team logo images for game branding; returns a public URL (relative /uploads/... or absolute Cloudinary URL).</summary>
public interface IOpponentLogoStorage
{
    /// <returns>Public URL path or absolute URL, or null if validation/storage failed.</returns>
    Task<string?> SaveOpponentLogoAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
