using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Administration.Commands.ReplaceRolePermissions;
using AppTorcedor.Application.Modules.Administration.Queries.ListAuditLogs;
using AppTorcedor.Application.Modules.Administration.Queries.ListRolePermissions;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public sealed class AdminGovernanceController(IMediator mediator) : ControllerBase
{
    [HttpGet("role-permissions")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ConfiguracoesVisualizar)]
    public async Task<IActionResult> RolePermissions(CancellationToken cancellationToken)
    {
        var rows = await mediator.Send(new ListRolePermissionsQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpGet("audit-logs")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ConfiguracoesVisualizar)]
    public async Task<IActionResult> AuditLogs([FromQuery] string? entityType, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var rows = await mediator.Send(new ListAuditLogsQuery(entityType, take), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpPut("role-permissions")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ConfiguracoesEditar)]
    public async Task<IActionResult> ReplaceRolePermissions([FromBody] ReplaceRolePermissionsRequest body, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Send(new ReplaceRolePermissionsCommand(body.RoleName, body.PermissionNames), cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
