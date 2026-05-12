using System.Security.Claims;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Commands.CancelMyBenefitRedemption;
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
    [HttpGet("shipping-options")]
    public async Task<ActionResult<IReadOnlyList<TorcedorShippingOptionResponse>>> GetShippingOptions(
        [FromQuery] string? cep,
        [FromServices] IMelhorEnvioShippingPort melhorEnvio,
        CancellationToken cancellationToken = default)
    {
        if (GetUserIdOrDefault() is null)
            return Unauthorized();

        var list = await melhorEnvio.CalculateAsync(cep ?? "", cancellationToken).ConfigureAwait(false);
        var res = list
            .Select(x => new TorcedorShippingOptionResponse(
                x.ServiceId,
                x.ServiceName,
                x.CarrierName,
                x.PictureUrl,
                x.Price,
                x.DeliveryDays))
            .ToList();
        return Ok(res);
    }

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
                dto.BannerUrl,
                dto.IsShirtCustomizationOffer,
                dto.ShirtSizes,
                dto.ShirtModels,
                dto.RedemptionWorkflowStatus,
                dto.RequiresApprovalForNextRedemption));
    }

    [HttpPost("offers/{offerId:guid}/redeem")]
    public async Task<ActionResult<object>> RedeemOffer(
        Guid offerId,
        [FromBody] TorcedorRedeemBenefitOfferRequest? body,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        TorcedorShirtRedemptionRequest? shirt = null;
        if (body is not null)
        {
            var hasAny =
                !string.IsNullOrWhiteSpace(body.ShirtSize)
                || !string.IsNullOrWhiteSpace(body.ShirtModel)
                || !string.IsNullOrWhiteSpace(body.ShirtNumber)
                || !string.IsNullOrWhiteSpace(body.ShirtDisplayName)
                || !string.IsNullOrWhiteSpace(body.DeliveryCep)
                || !string.IsNullOrWhiteSpace(body.DeliveryNeighborhood)
                || !string.IsNullOrWhiteSpace(body.DeliveryStreet)
                || !string.IsNullOrWhiteSpace(body.DeliveryNumber)
                || !string.IsNullOrWhiteSpace(body.DeliveryCity)
                || !string.IsNullOrWhiteSpace(body.DeliveryState)
                || !string.IsNullOrWhiteSpace(body.ShippingMethod)
                || body.ShippingCarrierId is not null
                || !string.IsNullOrWhiteSpace(body.ShippingCarrierName)
                || !string.IsNullOrWhiteSpace(body.ShippingServiceName)
                || body.ShippingPrice is not null
                || body.ShippingDeliveryDays is not null;
            if (hasAny)
            {
                shirt = new TorcedorShirtRedemptionRequest(
                    body.ShirtSize ?? "",
                    body.ShirtModel ?? "",
                    body.ShirtNumber ?? "",
                    body.ShirtDisplayName ?? "",
                    body.DeliveryCep ?? "",
                    body.DeliveryNeighborhood ?? "",
                    body.DeliveryStreet ?? "",
                    body.DeliveryNumber ?? "",
                    body.DeliveryCity ?? "",
                    body.DeliveryState ?? "",
                    body.ShippingMethod,
                    body.ShippingCarrierId,
                    body.ShippingCarrierName,
                    body.ShippingServiceName,
                    body.ShippingPrice,
                    body.ShippingDeliveryDays);
            }
        }

        var result = await mediator
            .Send(new RedeemBenefitOfferByTorcedorCommand(userId.Value, offerId, shirt), cancellationToken)
            .ConfigureAwait(false);

        if (result.Ok)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                new { redemptionId = result.RedemptionId, checkoutUrl = result.CheckoutUrl });
        }

        return result.Error switch
        {
            TorcedorRedemptionError.NotFound => NotFound(),
            TorcedorRedemptionError.NotEligible => BadRequest(new { error = "not_eligible" }),
            TorcedorRedemptionError.AlreadyRedeemed => BadRequest(new { error = "already_redeemed" }),
            TorcedorRedemptionError.Validation => BadRequest(new { error = "validation_failed" }),
            _ => BadRequest(),
        };
    }

    [HttpDelete("offers/{offerId:guid}/redemption")]
    public async Task<IActionResult> CancelRedemption(
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var result = await mediator
            .Send(new CancelMyBenefitRedemptionCommand(userId.Value, offerId), cancellationToken)
            .ConfigureAwait(false);

        if (result.Ok)
            return NoContent();

        return result.Error switch
        {
            TorcedorRedemptionCancelError.NotFound => NotFound(),
            TorcedorRedemptionCancelError.NotCancellable => Conflict(new { error = "not_cancellable" }),
            _ => BadRequest(),
        };
    }

    private Guid? GetUserIdOrDefault()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var g) ? g : null;
    }
}
