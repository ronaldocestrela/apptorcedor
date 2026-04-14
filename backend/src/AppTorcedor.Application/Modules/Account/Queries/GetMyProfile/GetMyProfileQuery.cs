using AppTorcedor.Application.Modules.Account;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Queries.GetMyProfile;

public sealed record GetMyProfileQuery(Guid UserId) : IRequest<MyProfileDto?>;
