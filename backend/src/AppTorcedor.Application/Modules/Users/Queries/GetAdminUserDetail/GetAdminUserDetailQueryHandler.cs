using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Queries.GetAdminUserDetail;

public sealed class GetAdminUserDetailQueryHandler(IUserAdministrationPort users)
    : IRequestHandler<GetAdminUserDetailQuery, AdminUserDetailDto?>
{
    public Task<AdminUserDetailDto?> Handle(GetAdminUserDetailQuery request, CancellationToken cancellationToken) =>
        users.GetUserDetailAsync(request.UserId, cancellationToken);
}
