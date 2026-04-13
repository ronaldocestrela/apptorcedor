using System.Security.Claims;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetMyLoyaltySummary;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetTorcedorAllTimeLoyaltyRanking;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetTorcedorMonthlyLoyaltyRanking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/loyalty")]
[Authorize]
public sealed class TorcedorLoyaltyController(IMediator mediator) : ControllerBase
{
    [HttpGet("me/summary")]
    public async Task<ActionResult<TorcedorLoyaltySummaryResponse>> MySummary(CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var dto = await mediator.Send(new GetMyLoyaltySummaryQuery(userId.Value), cancellationToken).ConfigureAwait(false);
        return Ok(
            new TorcedorLoyaltySummaryResponse(
                dto.TotalPoints,
                dto.MonthlyPoints,
                dto.MonthlyRank,
                dto.AllTimeRank,
                dto.AsOfUtc));
    }

    [HttpGet("rankings/monthly")]
    public async Task<ActionResult<TorcedorLoyaltyRankingPageResponse>> MonthlyRanking(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var dto = await mediator
            .Send(
                new GetTorcedorMonthlyLoyaltyRankingQuery(userId.Value, year, month, page, pageSize),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(MapPage(dto));
    }

    [HttpGet("rankings/all-time")]
    public async Task<ActionResult<TorcedorLoyaltyRankingPageResponse>> AllTimeRanking(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var dto = await mediator
            .Send(new GetTorcedorAllTimeLoyaltyRankingQuery(userId.Value, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(MapPage(dto));
    }

    private static TorcedorLoyaltyRankingPageResponse MapPage(LoyaltyTorcedorRankingPageDto dto)
    {
        var items = dto.Items
            .Select(i => new TorcedorLoyaltyRankingRowResponse(i.Rank, i.UserId, i.UserName, i.TotalPoints))
            .ToList();
        TorcedorLoyaltyMyStandingResponse? me = dto.Me is { } m
            ? new TorcedorLoyaltyMyStandingResponse(m.Rank, m.UserId, m.UserName, m.TotalPoints)
            : null;
        return new TorcedorLoyaltyRankingPageResponse(dto.TotalCount, items, me);
    }

    private Guid? GetUserIdOrDefault()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var g) ? g : null;
    }
}
