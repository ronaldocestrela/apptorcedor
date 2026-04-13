using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Commands.ExportUserData;

public sealed record ExportUserDataCommand(Guid SubjectUserId, Guid RequestedByUserId) : IRequest<PrivacyOperationResultDto>;
