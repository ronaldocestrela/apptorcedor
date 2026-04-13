using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.AnonymizeUser;

public sealed record AnonymizeUserCommand(Guid SubjectUserId, Guid RequestedByUserId) : IRequest<PrivacyOperationResultDto>;
