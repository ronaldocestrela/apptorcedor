using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Identity.Application.DTOs;

namespace SocioTorcedor.Modules.Identity.Application.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password) : ICommand<AuthResultDto>;
