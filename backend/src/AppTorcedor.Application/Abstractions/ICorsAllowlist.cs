namespace AppTorcedor.Application.Abstractions;

/// <summary>
/// Avalia se o header <c>Origin</c> das requisições está na união
/// de <c>Cors:AllowedOrigins</c> (env/appsettings) e de <c>Cors.AllowedOriginsExtra</c> (banco).
/// </summary>
public interface ICorsAllowlist
{
    /// <param name="origin">Valor do header <c>Origin</c> (pode ser <c>null</c> fora de CORS).</param>
    /// <returns>
    /// <c>true</c> se a origem for permitida; se não houver nenhuma origem configurada em ambas as fontes,
    /// repete o comportamento anterior: permite qualquer origem.
    /// </returns>
    bool IsOriginAllowed(string? origin);
}
