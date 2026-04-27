using System.Security.Claims;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.InvalidateDigitalCard;
using AppTorcedor.Application.Modules.Administration.Commands.IssueDigitalCard;
using AppTorcedor.Application.Modules.Administration.Commands.RegenerateDigitalCard;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminDigitalCardDetail;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminDigitalCards;
using AppTorcedor.Application.Modules.Administration.Queries.ListDigitalCardIssueCandidates;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/digital-cards")]
[Authorize]
public sealed class AdminDigitalCardsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.CarteirinhaVisualizar)]
    public async Task<ActionResult<AdminDigitalCardListPageDto>> List(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? membershipId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new ListAdminDigitalCardsQuery(userId, membershipId, status, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(pageDto);
    }

    [HttpGet("issue-candidates")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.CarteirinhaGerenciar)]
    public async Task<ActionResult<AdminDigitalCardIssueCandidatesPageDto>> ListIssueCandidates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new ListDigitalCardIssueCandidatesQuery(page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(pageDto);
    }

    [HttpGet("{digitalCardId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.CarteirinhaVisualizar)]
    public async Task<ActionResult<AdminDigitalCardDetailDto>> GetById(Guid digitalCardId, CancellationToken cancellationToken)
    {
        var detail = await mediator.Send(new GetAdminDigitalCardDetailQuery(digitalCardId), cancellationToken).ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("issue")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.CarteirinhaGerenciar)]
    public async Task<IActionResult> Issue([FromBody] IssueDigitalCardRequest body, CancellationToken cancellationToken)
    {
        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new IssueDigitalCardCommand(body.MembershipId, actor), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    [HttpPost("{digitalCardId:guid}/regenerate")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.CarteirinhaGerenciar)]
    public async Task<IActionResult> Regenerate(
        Guid digitalCardId,
        [FromBody] RegenerateDigitalCardRequest? body,
        CancellationToken cancellationToken)
    {
        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new RegenerateDigitalCardCommand(digitalCardId, body?.Reason, actor), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    [HttpPost("{digitalCardId:guid}/invalidate")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.CarteirinhaGerenciar)]
    public async Task<IActionResult> Invalidate(
        Guid digitalCardId,
        [FromBody] InvalidateDigitalCardRequest? body,
        CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Reason))
            return BadRequest(new { error = "Reason is required." });
        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new InvalidateDigitalCardCommand(digitalCardId, body.Reason, actor), cancellationToken)
            .ConfigureAwait(false);
        return MapMutation(result);
    }

    private IActionResult MapMutation(DigitalCardMutationResult result)
    {
        if (result.Ok)
            return NoContent();
        return result.Error switch
        {
            DigitalCardMutationError.NotFound => NotFound(),
            DigitalCardMutationError.MembershipNotEligible => BadRequest(new { error = "Membership is not eligible for this operation." }),
            DigitalCardMutationError.AlreadyHasActiveCard => Conflict(new { error = "An active digital card already exists for this membership." }),
            DigitalCardMutationError.InvalidTransition => BadRequest(new { error = "Invalid digital card status transition." }),
            DigitalCardMutationError.ReasonRequired => BadRequest(new { error = "Reason is required." }),
            DigitalCardMutationError.ReasonTooLong => BadRequest(new { error = "Reason is too long." }),
            _ => BadRequest(),
        };
    }

    private Guid GetUserIdOrThrow()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
            throw new InvalidOperationException("Authenticated user id is missing or invalid.");
        return guid;
    }
}
