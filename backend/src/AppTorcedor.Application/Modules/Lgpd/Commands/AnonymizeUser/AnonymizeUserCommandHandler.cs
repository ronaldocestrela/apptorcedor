using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.AnonymizeUser;

public sealed class AnonymizeUserCommandHandler(ILgpdAdministrationPort lgpd)
    : IRequestHandler<AnonymizeUserCommand, PrivacyOperationResultDto>
{
    public Task<PrivacyOperationResultDto> Handle(AnonymizeUserCommand request, CancellationToken cancellationToken) =>
        lgpd.AnonymizeUserAsync(request.SubjectUserId, request.RequestedByUserId, cancellationToken);
}
