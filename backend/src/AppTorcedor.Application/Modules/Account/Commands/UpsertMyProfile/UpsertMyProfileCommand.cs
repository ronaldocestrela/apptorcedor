using AppTorcedor.Application.Modules.Account;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Commands.UpsertMyProfile;

public sealed record UpsertMyProfileCommand(Guid UserId, MyProfileUpsertDto Patch) : IRequest<ProfileUpsertResult>;
