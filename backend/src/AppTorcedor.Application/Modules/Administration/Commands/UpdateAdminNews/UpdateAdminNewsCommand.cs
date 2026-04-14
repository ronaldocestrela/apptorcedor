using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminNews;

public sealed record UpdateAdminNewsCommand(Guid NewsId, AdminNewsWriteDto Dto) : IRequest<NewsMutationResult>;
