namespace AppTorcedor.Infrastructure.Options;

public sealed class OpponentLogoStorageOptions
{
    public const string SectionName = "OpponentLogos";

    /// <summary>Storage provider: Local or Cloudinary.</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Absolute path root for stored files. Empty = {ContentRoot}/wwwroot/uploads/opponent-logos.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>Max upload size in bytes (default 6 MB).</summary>
    public int MaxBytes { get; set; } = 6 * 1024 * 1024;

    public OpponentLogoCloudinaryOptions Cloudinary { get; set; } = new();
}

public sealed class OpponentLogoCloudinaryOptions
{
    public string Folder { get; set; } = "opponent-logos";
}
