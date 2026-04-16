using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Branding;
using MediatR;

namespace AppTorcedor.Application.Modules.Branding.Queries.GetPublicBranding;

public sealed class GetPublicBrandingQueryHandler(IAppConfigurationPort configuration)
    : IRequestHandler<GetPublicBrandingQuery, PublicBrandingDto>
{
    public async Task<PublicBrandingDto> Handle(GetPublicBrandingQuery request, CancellationToken cancellationToken)
    {
        var row = await configuration.GetAsync(BrandConfigurationKeys.TeamShieldUrl, cancellationToken).ConfigureAwait(false);
        var url = row is null || string.IsNullOrWhiteSpace(row.Value) ? null : row.Value.Trim();
        return new PublicBrandingDto(url);
    }
}
