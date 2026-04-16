using MediatR;

namespace AppTorcedor.Application.Modules.Branding.Commands.UploadTeamShield;

public sealed record UploadTeamShieldCommand(Stream Content, string FileName, string ContentType)
    : IRequest<UploadTeamShieldResult?>;

public sealed record UploadTeamShieldResult(string TeamShieldUrl);
