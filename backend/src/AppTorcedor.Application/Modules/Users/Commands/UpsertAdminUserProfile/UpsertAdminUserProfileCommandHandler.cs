using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Commands.UpsertAdminUserProfile;

public sealed class UpsertAdminUserProfileCommandHandler(IUserAdministrationPort users)
    : IRequestHandler<UpsertAdminUserProfileCommand, bool>
{
    public Task<bool> Handle(UpsertAdminUserProfileCommand request, CancellationToken cancellationToken) =>
        users.UpsertProfileAsync(request.UserId, request.Patch, cancellationToken);
}
