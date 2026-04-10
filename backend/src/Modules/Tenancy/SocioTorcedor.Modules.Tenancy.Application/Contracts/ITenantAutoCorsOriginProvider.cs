namespace SocioTorcedor.Modules.Tenancy.Application.Contracts;

/// <summary>
/// Resolve a origem HTTP cadastrada automaticamente em novos tenants para CORS dinâmico.
/// </summary>
public interface ITenantAutoCorsOriginProvider
{
    /// <param name="tenantSlug">Slug normalizado do tenant (lowercase).</param>
    /// <returns>Origem sem barra final.</returns>
    string GetDefaultOriginForNewTenant(string tenantSlug);
}
