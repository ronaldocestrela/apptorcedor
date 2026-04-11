namespace SocioTorcedor.Api.Options;

public sealed class BackofficeOptions
{
    public const string SectionName = "Backoffice";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Origens permitidas para CORS nas rotas <c>/api/backoffice/*</c> (SPA do backoffice em outra porta/host).
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = [];
}
