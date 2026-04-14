using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Commands.UpsertAdminUserProfile;

public sealed record UpsertAdminUserProfileCommand(Guid UserId, AdminUserProfileUpsertDto Patch) : IRequest<bool>;
