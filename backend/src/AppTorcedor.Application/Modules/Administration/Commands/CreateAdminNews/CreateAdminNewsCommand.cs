using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateAdminNews;

public sealed record CreateAdminNewsCommand(AdminNewsWriteDto Dto) : IRequest<NewsCreateResult>;
