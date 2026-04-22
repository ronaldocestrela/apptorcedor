using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Commands.UpsertAdminUserProfile;

public sealed record UpsertAdminUserProfileCommand(Guid UserId, AdminUserProfileUpsertDto Patch) : IRequest<ProfileUpsertResult>;
