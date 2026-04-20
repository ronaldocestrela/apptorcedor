using System.Security.Claims;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Commands.RedeemBenefitOfferByTorcedor;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetEligibleBenefitOfferDetail;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListEligibleBenefitOffers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/benefits")]
[Authorize]
public sealed class TorcedorBenefitsController(IMediator mediator) : ControllerBase
{
    [HttpGet("eligible")]
    public async Task<ActionResult<TorcedorEligibleBenefitOffersPageResponse>> ListEligible(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var pageDto = await mediator
            .Send(new ListEligibleBenefitOffersQuery(userId.Value, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        var items = pageDto.Items
            .Select(i => new TorcedorEligibleBenefitOfferResponse(
                i.OfferId,
                i.PartnerId,
                i.PartnerName,
                i.Title,
                i.Description,
                i.StartAt,
                i.EndAt,
                i.BannerUrl))
            .ToList();
        return Ok(new TorcedorEligibleBenefitOffersPageResponse(pageDto.TotalCount, items));
    }

    [HttpGet("offers/{offerId:guid}")]
    public async Task<ActionResult<TorcedorEligibleBenefitOfferDetailResponse>> GetOfferDetail(
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var dto = await mediator
            .Send(new GetEligibleBenefitOfferDetailQuery(userId.Value, offerId), cancellationToken)
            .ConfigureAwait(false);
        if (dto is null)
            return NotFound();

        return Ok(
            new TorcedorEligibleBenefitOfferDetailResponse(
                dto.OfferId,
                dto.PartnerId,
                dto.PartnerName,
                dto.Title,
                dto.Description,
                dto.StartAt,
                dto.EndAt,
                dto.AlreadyRedeemed,
                dto.RedemptionDateUtc,
                dto.BannerUrl));
    }

    [HttpPost("offers/{offerId:guid}/redeem")]
    public async Task<ActionResult<object>> RedeemOffer(
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var result = await mediator
            .Send(new RedeemBenefitOfferByTorcedorCommand(userId.Value, offerId), cancellationToken)
            .ConfigureAwait(false);

        if (result.Ok)
            return StatusCode(StatusCodes.Status201Created, new { redemptionId = result.RedemptionId });

        return result.Error switch
        {
            TorcedorRedemptionError.NotFound => NotFound(),
            TorcedorRedemptionError.NotEligible => BadRequest(new { error = "not_eligible" }),
            TorcedorRedemptionError.AlreadyRedeemed => BadRequest(new { error = "already_redeemed" }),
            _ => BadRequest(),
        };
    }

    private Guid? GetUserIdOrDefault()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var g) ? g : null;
    }
}
