using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Commands.UpsertAdminUserProfile;

public sealed class UpsertAdminUserProfileCommandHandler(IUserAdministrationPort users)
    : IRequestHandler<UpsertAdminUserProfileCommand, ProfileUpsertResult>
{
    public Task<ProfileUpsertResult> Handle(UpsertAdminUserProfileCommand request, CancellationToken cancellationToken) =>
        users.UpsertProfileAsync(request.UserId, request.Patch, cancellationToken);
}
