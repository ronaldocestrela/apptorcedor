using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListTorcedorGames;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/games")]
[Authorize]
public sealed class TorcedorGamesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TorcedorGameListPageResponse>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new ListTorcedorGamesQuery(search, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        var items = pageDto.Items
            .Select(i => new TorcedorGameListItemResponse(
                i.GameId,
                i.Opponent,
                i.Competition,
                i.GameDate,
                i.CreatedAt))
            .ToList();
        return Ok(new TorcedorGameListPageResponse(pageDto.TotalCount, items));
    }
}
