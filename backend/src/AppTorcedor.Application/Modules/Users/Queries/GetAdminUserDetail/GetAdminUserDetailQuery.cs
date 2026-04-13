using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Queries.GetAdminUserDetail;

public sealed record GetAdminUserDetailQuery(Guid UserId) : IRequest<AdminUserDetailDto?>;
