using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Identity.Application.Contracts;
using SocioTorcedor.Modules.Identity.Application.DTOs;

namespace SocioTorcedor.Modules.Identity.Application.Commands.LoginUser;

public sealed class LoginUserHandler(IIdentityService identityService)
    : ICommandHandler<LoginUserCommand, AuthResultDto>
{
    public Task<Result<AuthResultDto>> Handle(
        LoginUserCommand command,
        CancellationToken cancellationToken) =>
        identityService.LoginAsync(command.Email, command.Password, cancellationToken);
}
