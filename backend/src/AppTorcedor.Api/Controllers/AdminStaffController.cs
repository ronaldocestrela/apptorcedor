using System.Security.Claims;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Administration.Commands.CreateStaffInvite;
using AppTorcedor.Application.Modules.Administration.Commands.ReplaceStaffUserRoles;
using AppTorcedor.Application.Modules.Administration.Commands.SetStaffUserActive;
using AppTorcedor.Application.Modules.Administration.Queries.ListStaffInvites;
using AppTorcedor.Application.Modules.Administration.Queries.ListStaffUsers;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/staff")]
[Authorize]
public sealed class AdminStaffController(IMediator mediator) : ControllerBase
{
    [HttpPost("invites")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosEditar)]
    public async Task<ActionResult<CreateStaffInviteResponse>> CreateInvite(
        [FromBody] CreateStaffInviteRequest body,
        CancellationToken cancellationToken)
    {
        var actorId = GetUserIdOrThrow();
        try
        {
            var created = await mediator
                .Send(new CreateStaffInviteCommand(body.Email, body.Name, body.Roles, actorId), cancellationToken)
                .ConfigureAwait(false);
            return Ok(new CreateStaffInviteResponse(created.Id, created.Token, created.ExpiresAt));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("invites")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosVisualizar)]
    public async Task<IActionResult> ListInvites(CancellationToken cancellationToken)
    {
        var rows = await mediator.Send(new ListStaffInvitesQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpGet("users")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosVisualizar)]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        var rows = await mediator.Send(new ListStaffUsersQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpPatch("users/{userId:guid}/active")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosEditar)]
    public async Task<IActionResult> SetActive(Guid userId, [FromBody] SetStaffUserActiveRequest body, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new SetStaffUserActiveCommand(userId, body.IsActive), cancellationToken).ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }

    [HttpPut("users/{userId:guid}/roles")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosEditar)]
    public async Task<IActionResult> ReplaceRoles(Guid userId, [FromBody] ReplaceStaffUserRolesRequest body, CancellationToken cancellationToken)
    {
        try
        {
            var ok = await mediator.Send(new ReplaceStaffUserRolesCommand(userId, body.Roles), cancellationToken).ConfigureAwait(false);
            return ok ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid GetUserIdOrThrow()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
            throw new InvalidOperationException("Missing user id.");
        return guid;
    }
}
