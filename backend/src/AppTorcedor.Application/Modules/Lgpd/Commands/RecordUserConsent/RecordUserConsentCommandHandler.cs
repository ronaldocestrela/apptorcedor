using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.RecordUserConsent;

public sealed class RecordUserConsentCommandHandler(ILgpdAdministrationPort lgpd)
    : IRequestHandler<RecordUserConsentCommand, Unit>
{
    public async Task<Unit> Handle(RecordUserConsentCommand request, CancellationToken cancellationToken)
    {
        await lgpd.RecordConsentAsync(request.UserId, request.DocumentVersionId, request.ClientIp, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
