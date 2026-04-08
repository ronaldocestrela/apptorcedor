using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.Modules.Identity.Application.Commands.LoginUser;
using SocioTorcedor.Modules.Identity.Application.Commands.RegisterUser;

namespace SocioTorcedor.Modules.Identity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    public sealed class RegisterBody
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;
    }

    public sealed class LoginBody
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RegisterUserCommand(body.Email, body.Password, body.FirstName, body.LastName),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { code = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginUserCommand(body.Email, body.Password), cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new { code = result.Error!.Code, message = result.Error.Message });

        return Ok(result.Value);
    }
}
