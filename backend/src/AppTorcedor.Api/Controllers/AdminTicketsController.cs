using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.PurchaseAdminTicket;
using AppTorcedor.Application.Modules.Administration.Commands.RedeemAdminTicket;
using AppTorcedor.Application.Modules.Administration.Commands.ReserveAdminTicket;
using AppTorcedor.Application.Modules.Administration.Commands.SyncAdminTicket;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminTicketRequestStatus;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminTicket;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminTickets;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/tickets")]
[Authorize]
public sealed class AdminTicketsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.IngressosVisualizar)]
    public async Task<ActionResult<AdminTicketListPageDto>> List(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? gameId,
        [FromQuery] string? status,
        [FromQuery] string? requestStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new ListAdminTicketsQuery(userId, gameId, status, requestStatus, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(pageDto);
    }

    [HttpGet("{ticketId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.IngressosVisualizar)]
    public async Task<ActionResult<AdminTicketDetailDto>> GetById(Guid ticketId, CancellationToken cancellationToken)
    {
        var detail = await mediator.Send(new GetAdminTicketQuery(ticketId), cancellationToken).ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("reserve")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.IngressosGerenciar)]
    public async Task<ActionResult<object>> Reserve([FromBody] ReserveTicketRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await mediator
            .Send(new ReserveAdminTicketCommand(body.UserId, body.GameId), cancellationToken)
            .ConfigureAwait(false);
        if (!result.Ok)
            return BadRequest(new { error = MapReserveError(result.Error) });

        return CreatedAtAction(nameof(GetById), new { ticketId = result.TicketId }, new { ticketId = result.TicketId });
    }

    [HttpPost("{ticketId:guid}/purchase")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.IngressosGerenciar)]
    public async Task<IActionResult> Purchase(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PurchaseAdminTicketCommand(ticketId), cancellationToken).ConfigureAwait(false);
        return MapTicketMutation(result);
    }

    [HttpPost("{ticketId:guid}/sync")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.IngressosGerenciar)]
    public async Task<IActionResult> Sync(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SyncAdminTicketCommand(ticketId), cancellationToken).ConfigureAwait(false);
        return MapTicketMutation(result);
    }

    [HttpPost("{ticketId:guid}/redeem")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.IngressosGerenciar)]
    public async Task<IActionResult> Redeem(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RedeemAdminTicketCommand(ticketId), cancellationToken).ConfigureAwait(false);
        return MapTicketMutation(result);
    }

    [HttpPatch("{ticketId:guid}/request-status")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.IngressosGerenciar)]
    public async Task<IActionResult> UpdateRequestStatus(
        Guid ticketId,
        [FromBody] UpdateTicketRequestStatusRequest body,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await mediator
            .Send(new UpdateAdminTicketRequestStatusCommand(ticketId, body.RequestStatus), cancellationToken)
            .ConfigureAwait(false);
        return MapRequestStatusUpdate(result);
    }

    private static string MapReserveError(TicketMutationError? error) =>
        error switch
        {
            TicketMutationError.GameNotFound => "Game not found.",
            TicketMutationError.UserNotFound => "User not found.",
            TicketMutationError.GameInactive => "Game is not active.",
            TicketMutationError.ProviderError => "Ticket provider error.",
            _ => "Unable to reserve ticket.",
        };

    private IActionResult MapTicketMutation(TicketMutationResult result)
    {
        if (result.Ok)
            return NoContent();
        return result.Error switch
        {
            TicketMutationError.NotFound => NotFound(),
            TicketMutationError.GameNotFound => BadRequest(new { error = "Game not found." }),
            TicketMutationError.UserNotFound => BadRequest(new { error = "User not found." }),
            TicketMutationError.GameInactive => BadRequest(new { error = "Game is not active." }),
            TicketMutationError.InvalidTransition => BadRequest(new { error = "Invalid ticket status transition." }),
            TicketMutationError.ExternalIdMissing => BadRequest(new { error = "External ticket id is missing." }),
            TicketMutationError.ProviderError => BadRequest(new { error = "Ticket provider error." }),
            _ => BadRequest(),
        };
    }

    private IActionResult MapRequestStatusUpdate(TicketMutationResult result)
    {
        if (result.Ok)
            return NoContent();
        return result.Error switch
        {
            TicketMutationError.NotFound => NotFound(),
            TicketMutationError.InvalidRequestStatus => BadRequest(new { error = "Invalid requestStatus. Use Pending or Issued." }),
            _ => BadRequest(),
        };
    }
}
