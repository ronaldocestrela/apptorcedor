using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateStaffInvite;

public sealed class CreateStaffInviteCommandHandler(IStaffAdministrationPort staff)
    : IRequestHandler<CreateStaffInviteCommand, StaffInviteCreatedDto>
{
    public Task<StaffInviteCreatedDto> Handle(CreateStaffInviteCommand request, CancellationToken cancellationToken) =>
        staff.CreateInviteAsync(request.Email, request.Name, request.Roles, request.CreatedByUserId, cancellationToken);
}
