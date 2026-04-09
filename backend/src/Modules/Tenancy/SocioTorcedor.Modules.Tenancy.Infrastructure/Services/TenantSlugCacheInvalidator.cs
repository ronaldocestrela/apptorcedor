using Microsoft.Extensions.Caching.Memory;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

public sealed class TenantSlugCacheInvalidator(IMemoryCache cache) : ITenantSlugCacheInvalidator
{
    public void Invalidate(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return;

        var key = $"tenant:slug:{slug.Trim().ToLowerInvariant()}";
        cache.Remove(key);
    }
}
