namespace AppTorcedor.Api.Contracts;

public sealed class PublicBrandingResponse
{
    public string? TeamShieldUrl { get; init; }
}

public sealed class TeamShieldUploadResponse
{
    public string TeamShieldUrl { get; init; } = string.Empty;
}
