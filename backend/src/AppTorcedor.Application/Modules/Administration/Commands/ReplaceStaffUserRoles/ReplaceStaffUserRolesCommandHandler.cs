using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReplaceStaffUserRoles;

public sealed class ReplaceStaffUserRolesCommandHandler(IStaffAdministrationPort staff)
    : IRequestHandler<ReplaceStaffUserRolesCommand, bool>
{
    public Task<bool> Handle(ReplaceStaffUserRolesCommand request, CancellationToken cancellationToken) =>
        staff.ReplaceUserRolesAsync(request.UserId, request.Roles, cancellationToken);
}
