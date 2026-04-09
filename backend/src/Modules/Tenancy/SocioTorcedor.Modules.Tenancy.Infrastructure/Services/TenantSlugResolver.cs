using Microsoft.Extensions.Caching.Memory;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;

namespace SocioTorcedor.Modules.Tenancy.Infrastructure.Services;

public sealed class TenantSlugResolver(
    ITenantRepository repository,
    IMemoryCache cache) : ITenantResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<TenantContext?> ResolveAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var key = $"tenant:slug:{slug.Trim().ToLowerInvariant()}";
        if (cache.TryGetValue(key, out TenantContext? cached))
            return cached;

        var dto = await repository.GetBySlugAsync(slug, cancellationToken);
        if (dto is null)
            return null;

        var context = new TenantContext(
            dto.TenantId,
            dto.Name,
            dto.Slug,
            dto.ConnectionString,
            dto.AllowedOrigins);

        cache.Set(key, context, CacheDuration);
        return context;
    }
}
