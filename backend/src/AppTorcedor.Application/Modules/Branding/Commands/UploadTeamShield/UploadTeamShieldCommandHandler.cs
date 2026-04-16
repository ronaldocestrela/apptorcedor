using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Branding;
using MediatR;

namespace AppTorcedor.Application.Modules.Branding.Commands.UploadTeamShield;

public sealed class UploadTeamShieldCommandHandler(ITeamShieldStorage storage, IAppConfigurationPort configuration)
    : IRequestHandler<UploadTeamShieldCommand, UploadTeamShieldResult?>
{
    public async Task<UploadTeamShieldResult?> Handle(UploadTeamShieldCommand request, CancellationToken cancellationToken)
    {
        var previous = await configuration.GetAsync(BrandConfigurationKeys.TeamShieldUrl, cancellationToken).ConfigureAwait(false);
        var previousUrl = string.IsNullOrWhiteSpace(previous?.Value) ? null : previous.Value.Trim();

        var url = await storage
            .SaveTeamShieldAsync(request.Content, request.FileName, request.ContentType, cancellationToken)
            .ConfigureAwait(false);
        if (url is null)
            return null;

        await configuration.UpsertAsync(BrandConfigurationKeys.TeamShieldUrl, url, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(previousUrl)
            && !string.Equals(previousUrl, url, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await storage.DeleteTeamShieldAsync(previousUrl!, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                /* best-effort cleanup */
            }
        }

        return new UploadTeamShieldResult(url);
    }
}
