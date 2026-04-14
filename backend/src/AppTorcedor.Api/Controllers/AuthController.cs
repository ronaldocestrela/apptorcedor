using AppTorcedor.Api.Contracts;
using AppTorcedor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request.Email, request.Password, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return Unauthorized();
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.RefreshAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return Unauthorized();
        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var me = await auth.GetMeAsync(User, cancellationToken).ConfigureAwait(false);
        if (me is null)
            return Unauthorized();
        return Ok(me);
    }

    [HttpPost("accept-staff-invite")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> AcceptStaffInvite(
        [FromBody] AcceptStaffInviteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await auth.AcceptStaffInviteAsync(request.Token, request.Password, request.Name, cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
            return Unauthorized();
        return Ok(result);
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Google([FromBody] GoogleSignInRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.SignInWithGoogleAsync(request, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return Unauthorized();
        return Ok(result);
    }
}
