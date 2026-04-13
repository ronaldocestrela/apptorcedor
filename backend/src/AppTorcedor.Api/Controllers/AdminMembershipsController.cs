using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateMembershipStatus;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/memberships")]
[Authorize]
public sealed class AdminMembershipsController(IMediator mediator) : ControllerBase
{
    [HttpPatch("{membershipId:guid}/status")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.SociosGerenciar)]
    public async Task<IActionResult> PatchStatus(Guid membershipId, [FromBody] UpdateMembershipStatusRequest body, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new UpdateMembershipStatusCommand(membershipId, body.Status), cancellationToken).ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }
}
