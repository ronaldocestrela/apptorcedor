namespace SocioTorcedor.Api.Options;

public sealed class BackofficeOptions
{
    public const string SectionName = "Backoffice";

    public string ApiKey { get; set; } = string.Empty;
}
