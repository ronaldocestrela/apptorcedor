using System.Security.Claims;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateMembershipStatus;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminMembershipDetail;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminMemberships;
using AppTorcedor.Application.Modules.Administration.Queries.ListMembershipHistory;
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
    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.SociosGerenciar)]
    public async Task<ActionResult<AdminMembershipListPageDto>> List(
        [FromQuery] MembershipStatus? status,
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new ListAdminMembershipsQuery(status, userId, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(pageDto);
    }

    [HttpGet("{membershipId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.SociosGerenciar)]
    public async Task<ActionResult<AdminMembershipDetailDto>> GetById(Guid membershipId, CancellationToken cancellationToken)
    {
        var detail = await mediator.Send(new GetAdminMembershipDetailQuery(membershipId), cancellationToken).ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("{membershipId:guid}/history")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.SociosGerenciar)]
    public async Task<ActionResult<IReadOnlyList<MembershipHistoryEventDto>>> History(
        Guid membershipId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var exists = await mediator.Send(new GetAdminMembershipDetailQuery(membershipId), cancellationToken).ConfigureAwait(false);
        if (exists is null)
            return NotFound();
        var rows = await mediator.Send(new ListMembershipHistoryQuery(membershipId, take), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpPatch("{membershipId:guid}/status")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.SociosGerenciar)]
    public async Task<IActionResult> PatchStatus(Guid membershipId, [FromBody] UpdateMembershipStatusRequest body, CancellationToken cancellationToken)
    {
        var actor = GetUserIdOrThrow();
        var result = await mediator.Send(
                new UpdateMembershipStatusCommand(membershipId, body.Status, body.Reason.Trim(), actor),
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.Ok)
        {
            return result.Error switch
            {
                MembershipStatusUpdateError.NotFound => NotFound(),
                MembershipStatusUpdateError.Unchanged => BadRequest(new { error = "Status unchanged." }),
                _ => BadRequest(),
            };
        }

        return NoContent();
    }

    private Guid GetUserIdOrThrow()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
            throw new InvalidOperationException("Authenticated user id is missing or invalid.");
        return guid;
    }
}
