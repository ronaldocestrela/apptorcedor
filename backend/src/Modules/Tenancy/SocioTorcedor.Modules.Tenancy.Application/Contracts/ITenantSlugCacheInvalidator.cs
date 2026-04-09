namespace SocioTorcedor.Modules.Tenancy.Application.Contracts;

public interface ITenantSlugCacheInvalidator
{
    void Invalidate(string slug);
}
