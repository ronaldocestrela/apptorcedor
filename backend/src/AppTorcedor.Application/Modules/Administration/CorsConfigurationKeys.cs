namespace AppTorcedor.Application.Modules.Administration;

/// <summary>
/// Chave em <c>AppConfigurationEntries</c> (Tipo B) para origens CORS adicionais.
/// O valor unifica JSON array (preferencial), linhas e CSV.
/// </summary>
public static class CorsConfigurationKeys
{
    public const string AllowedOriginsExtra = "Cors.AllowedOriginsExtra";
}
