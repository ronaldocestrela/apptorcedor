using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.SetStaffUserActive;

public sealed class SetStaffUserActiveCommandHandler(IStaffAdministrationPort staff)
    : IRequestHandler<SetStaffUserActiveCommand, bool>
{
    public Task<bool> Handle(SetStaffUserActiveCommand request, CancellationToken cancellationToken) =>
        staff.SetUserActiveAsync(request.UserId, request.IsActive, cancellationToken);
}
