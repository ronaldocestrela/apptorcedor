using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListStaffInvites;

public sealed record ListStaffInvitesQuery : IRequest<IReadOnlyList<StaffInviteRowDto>>;
