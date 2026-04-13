using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UnpublishAdminNews;

public sealed record UnpublishAdminNewsCommand(Guid NewsId) : IRequest<NewsMutationResult>;
