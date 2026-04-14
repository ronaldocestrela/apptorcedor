using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Commands.SetUserAccountActive;

public sealed class SetUserAccountActiveCommandHandler(IUserAdministrationPort users)
    : IRequestHandler<SetUserAccountActiveCommand, bool>
{
    public Task<bool> Handle(SetUserAccountActiveCommand request, CancellationToken cancellationToken) =>
        users.SetAccountActiveAsync(request.UserId, request.IsActive, cancellationToken);
}
