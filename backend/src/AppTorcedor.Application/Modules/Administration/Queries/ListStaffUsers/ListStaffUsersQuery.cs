using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListStaffUsers;

public sealed record ListStaffUsersQuery : IRequest<IReadOnlyList<StaffUserRowDto>>;
