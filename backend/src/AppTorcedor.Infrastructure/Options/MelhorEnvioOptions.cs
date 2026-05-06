namespace AppTorcedor.Infrastructure.Options;

public sealed class MelhorEnvioOptions
{
    public const string SectionName = "MelhorEnvio";

    /// <summary>Token OAuth Melhor Envio (Bearer). Vazio = API retorna lista vazia.</summary>
    public string Token { get; set; } = "";

    public string UserAgent { get; set; } = "";

    /// <summary>CEP de origem (apenas dígitos).</summary>
    public string FromPostalCode { get; set; } = "";

    public string BaseUrl { get; set; } = "https://www.melhorenvio.com.br";

    public int PackageHeight { get; set; } = 4;

    public int PackageWidth { get; set; } = 12;

    public int PackageLength { get; set; } = 17;

    /// <summary>Peso em kg.</summary>
    public decimal PackageWeight { get; set; } = 0.3m;
}
