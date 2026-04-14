using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.RecordUserConsent;

public sealed record RecordUserConsentCommand(Guid UserId, Guid DocumentVersionId, string? ClientIp) : IRequest<Unit>;
