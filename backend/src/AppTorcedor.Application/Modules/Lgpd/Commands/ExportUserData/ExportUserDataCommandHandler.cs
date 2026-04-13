using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.ExportUserData;

public sealed class ExportUserDataCommandHandler(ILgpdAdministrationPort lgpd)
    : IRequestHandler<ExportUserDataCommand, PrivacyOperationResultDto>
{
    public Task<PrivacyOperationResultDto> Handle(ExportUserDataCommand request, CancellationToken cancellationToken) =>
        lgpd.ExportUserDataAsync(request.SubjectUserId, request.RequestedByUserId, cancellationToken);
}
