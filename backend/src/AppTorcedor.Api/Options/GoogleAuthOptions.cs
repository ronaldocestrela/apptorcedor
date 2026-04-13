namespace AppTorcedor.Api.Options;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Google:Auth";

    /// <summary>OAuth2.0 Client ID (Web) used to validate Google ID tokens.</summary>
    public string ClientId { get; set; } = string.Empty;
}
