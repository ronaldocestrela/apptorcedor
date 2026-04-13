using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Commands.UpsertMyProfile;

public sealed class UpsertMyProfileCommandHandler(ITorcedorAccountPort account)
    : IRequestHandler<UpsertMyProfileCommand, bool>
{
    public Task<bool> Handle(UpsertMyProfileCommand request, CancellationToken cancellationToken) =>
        account.UpsertProfileAsync(request.UserId, request.Patch, cancellationToken);
}
