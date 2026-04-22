using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppTorcedor.Infrastructure.Services.Cors;

public sealed class CorsAllowlist(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<CorsAllowlist> log) : ICorsAllowlist
{
    private readonly object _sync = new();
    private IReadOnlyList<string> _cachedDynamic = Array.Empty<string>();
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;

    public bool IsOriginAllowed(string? origin)
    {
        var staticNormalized = NormalizeStaticOrigins();
        var dynamicNormalized = GetCachedDynamicOrigins();
        var merged = staticNormalized
            .Concat(dynamicNormalized)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (merged.Count is 0)
            return true;

        var n = CorsAllowedOriginsParser.TryNormalizeOrigin(origin);
        if (n is null)
            return false;

        return merged.Contains(n, StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyList<string> NormalizeStaticOrigins()
    {
        var raw = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        return raw
            .Select(CorsAllowedOriginsParser.TryNormalizeOrigin)
            .Where(x => x is not null)
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<string> GetCachedDynamicOrigins()
    {
        var ttl = GetCacheTtl();
        var now = TimeProvider.System.GetUtcNow();

        lock (_sync)
        {
            if (ttl > TimeSpan.Zero && now - _cachedAt < ttl)
                return _cachedDynamic;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var port = scope.ServiceProvider.GetRequiredService<IAppConfigurationPort>();
                var entry = port
                    .GetAsync(CorsConfigurationKeys.AllowedOriginsExtra, CancellationToken.None)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
                var value = entry?.Value;
                var parsed = CorsAllowedOriginsParser.Parse(
                    value,
                    (raw, ex) => log.LogWarning(
                        ex,
                        "Cors.AllowedOriginsExtra inválido ou JSON malformado; tentando fallback de parse. Valor (início): {Preview}",
                        raw.Length > 120 ? raw[..120] : raw));
                _cachedDynamic = parsed;
                _cachedAt = now;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Falha ao ler Cors.AllowedOriginsExtra; mantendo cache anterior ou vazio.");
                if (_cachedAt == DateTimeOffset.MinValue)
                    _cachedDynamic = Array.Empty<string>();
                _cachedAt = now;
            }

            return _cachedDynamic;
        }
    }

    private TimeSpan GetCacheTtl()
    {
        var sec = configuration.GetValue("Cors:DynamicOriginsCacheSeconds", 60);
        if (sec <= 0)
            return TimeSpan.Zero;
        return TimeSpan.FromSeconds(sec);
    }
}
