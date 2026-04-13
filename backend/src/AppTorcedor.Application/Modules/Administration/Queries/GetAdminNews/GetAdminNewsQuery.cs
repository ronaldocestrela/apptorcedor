using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminNews;

public sealed record GetAdminNewsQuery(Guid NewsId) : IRequest<AdminNewsDetailDto?>;
