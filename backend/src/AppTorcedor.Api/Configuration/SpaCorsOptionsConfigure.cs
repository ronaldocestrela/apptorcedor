using AppTorcedor.Application.Abstractions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Api.Configuration;

public sealed class SpaCorsOptionsConfigure(ICorsAllowlist allowlist) : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        options.AddPolicy(
            "Spa",
            p => p
                .SetIsOriginAllowed(allowlist.IsOriginAllowed)
                .AllowAnyHeader()
                .AllowAnyMethod());
    }
}
