using System.Security.Claims;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Commands.RedeemMyTicket;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetMyTicket;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListMyTickets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TorcedorTicketsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TorcedorTicketListPageResponse>> List(
        [FromQuery] Guid? gameId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();
        var pageDto = await mediator
            .Send(new ListMyTicketsQuery(userId.Value, gameId, status, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        var items = pageDto.Items
            .Select(i => new TorcedorTicketListItemResponse(
                i.TicketId,
                i.GameId,
                i.Opponent,
                i.Competition,
                i.GameDate,
                i.Status,
                i.ExternalTicketId,
                i.QrCode,
                i.CreatedAt,
                i.RedeemedAt))
            .ToList();
        return Ok(new TorcedorTicketListPageResponse(pageDto.TotalCount, items));
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<ActionResult<TorcedorTicketDetailResponse>> GetById(Guid ticketId, CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();
        var d = await mediator.Send(new GetMyTicketQuery(userId.Value, ticketId), cancellationToken).ConfigureAwait(false);
        if (d is null)
            return NotFound();
        return Ok(
            new TorcedorTicketDetailResponse(
                d.TicketId,
                d.GameId,
                d.Opponent,
                d.Competition,
                d.GameDate,
                d.Status,
                d.ExternalTicketId,
                d.QrCode,
                d.CreatedAt,
                d.UpdatedAt,
                d.RedeemedAt));
    }

    [HttpPost("{ticketId:guid}/redeem")]
    public async Task<IActionResult> Redeem(Guid ticketId, CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();
        var result = await mediator
            .Send(new RedeemMyTicketCommand(userId.Value, ticketId), cancellationToken)
            .ConfigureAwait(false);
        return MapTicketMutation(result);
    }

    private Guid? GetUserIdOrDefault()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var g) ? g : null;
    }

    private IActionResult MapTicketMutation(TicketMutationResult result)
    {
        if (result.Ok)
            return NoContent();
        return result.Error switch
        {
            TicketMutationError.NotFound => NotFound(),
            TicketMutationError.InvalidTransition => BadRequest(new { error = "Invalid ticket status transition." }),
            TicketMutationError.ProviderError => BadRequest(new { error = "Ticket provider error." }),
            _ => BadRequest(),
        };
    }
}
