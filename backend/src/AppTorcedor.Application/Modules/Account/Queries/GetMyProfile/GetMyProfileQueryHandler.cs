using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler(ITorcedorAccountPort account)
    : IRequestHandler<GetMyProfileQuery, MyProfileDto?>
{
    public Task<MyProfileDto?> Handle(GetMyProfileQuery request, CancellationToken cancellationToken) =>
        account.GetProfileAsync(request.UserId, cancellationToken);
}
