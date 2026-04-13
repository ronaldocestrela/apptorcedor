using System.Security.Claims;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreateBenefitOffer;
using AppTorcedor.Application.Modules.Administration.Commands.CreateBenefitPartner;
using AppTorcedor.Application.Modules.Administration.Commands.RedeemBenefitOffer;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateBenefitOffer;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateBenefitPartner;
using AppTorcedor.Application.Modules.Administration.Queries.GetBenefitOffer;
using AppTorcedor.Application.Modules.Administration.Queries.GetBenefitPartner;
using AppTorcedor.Application.Modules.Administration.Queries.ListBenefitOffers;
using AppTorcedor.Application.Modules.Administration.Queries.ListBenefitPartners;
using AppTorcedor.Application.Modules.Administration.Queries.ListBenefitRedemptions;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/benefits")]
[Authorize]
public sealed class AdminBenefitsController(IMediator mediator) : ControllerBase
{
    [HttpGet("partners")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosVisualizar)]
    public async Task<ActionResult<BenefitPartnerListPageDto>> ListPartners(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator
            .Send(new ListBenefitPartnersQuery(search, isActive, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("partners/{partnerId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosVisualizar)]
    public async Task<ActionResult<BenefitPartnerDetailDto>> GetPartner(Guid partnerId, CancellationToken cancellationToken)
    {
        var d = await mediator.Send(new GetBenefitPartnerQuery(partnerId), cancellationToken).ConfigureAwait(false);
        return d is null ? NotFound() : Ok(d);
    }

    [HttpPost("partners")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosGerenciar)]
    public async Task<ActionResult<object>> CreatePartner([FromBody] UpsertBenefitPartnerRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var dto = MapPartner(body);
        var result = await mediator.Send(new CreateBenefitPartnerCommand(dto), cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
            return BadRequest(new { error = "Validation failed." });

        return CreatedAtAction(nameof(GetPartner), new { partnerId = result.Id }, new { partnerId = result.Id });
    }

    [HttpPut("partners/{partnerId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosGerenciar)]
    public async Task<IActionResult> UpdatePartner(
        Guid partnerId,
        [FromBody] UpsertBenefitPartnerRequest body,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await mediator
            .Send(new UpdateBenefitPartnerCommand(partnerId, MapPartner(body)), cancellationToken)
            .ConfigureAwait(false);
        return MapBenefitMutation(result);
    }

    [HttpGet("offers")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosVisualizar)]
    public async Task<ActionResult<BenefitOfferListPageDto>> ListOffers(
        [FromQuery] Guid? partnerId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator
            .Send(new ListBenefitOffersQuery(partnerId, isActive, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpGet("offers/{offerId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosVisualizar)]
    public async Task<ActionResult<BenefitOfferDetailDto>> GetOffer(Guid offerId, CancellationToken cancellationToken)
    {
        var d = await mediator.Send(new GetBenefitOfferQuery(offerId), cancellationToken).ConfigureAwait(false);
        return d is null ? NotFound() : Ok(d);
    }

    [HttpPost("offers")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosGerenciar)]
    public async Task<ActionResult<object>> CreateOffer([FromBody] UpsertBenefitOfferRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var dto = MapOffer(body);
        var result = await mediator.Send(new CreateBenefitOfferCommand(dto), cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
            return BadRequest(new { error = "Validation failed." });

        return CreatedAtAction(nameof(GetOffer), new { offerId = result.Id }, new { offerId = result.Id });
    }

    [HttpPut("offers/{offerId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosGerenciar)]
    public async Task<IActionResult> UpdateOffer(
        Guid offerId,
        [FromBody] UpsertBenefitOfferRequest body,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await mediator
            .Send(new UpdateBenefitOfferCommand(offerId, MapOffer(body)), cancellationToken)
            .ConfigureAwait(false);
        return MapBenefitMutation(result);
    }

    [HttpPost("offers/{offerId:guid}/redeem")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosGerenciar)]
    public async Task<ActionResult<object>> Redeem(
        Guid offerId,
        [FromBody] RedeemBenefitOfferRequest body,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var actor = GetUserIdOrThrow();
        var result = await mediator
            .Send(new RedeemBenefitOfferCommand(offerId, body.UserId, body.Notes, actor), cancellationToken)
            .ConfigureAwait(false);

        if (result.Ok)
            return StatusCode(201, new { redemptionId = result.RedemptionId });

        return result.Error switch
        {
            BenefitMutationError.NotFound => NotFound(),
            BenefitMutationError.InvalidState => BadRequest(new { error = "Offer not active or outside validity window." }),
            BenefitMutationError.Validation => BadRequest(new { error = "Validation failed." }),
            _ => BadRequest(),
        };
    }

    [HttpGet("redemptions")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.BeneficiosVisualizar)]
    public async Task<ActionResult<BenefitRedemptionListPageDto>> ListRedemptions(
        [FromQuery] Guid? offerId,
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var dto = await mediator
            .Send(new ListBenefitRedemptionsQuery(offerId, userId, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(dto);
    }

    private static BenefitPartnerWriteDto MapPartner(UpsertBenefitPartnerRequest body) =>
        new(body.Name, body.Description, body.IsActive);

    private static BenefitOfferWriteDto MapOffer(UpsertBenefitOfferRequest body) =>
        new(
            body.PartnerId,
            body.Title,
            body.Description,
            body.IsActive,
            body.StartAt,
            body.EndAt,
            body.EligiblePlanIds,
            body.EligibleMembershipStatuses);

    private IActionResult MapBenefitMutation(BenefitMutationResult result)
    {
        if (result.Ok)
            return NoContent();
        return result.Error switch
        {
            BenefitMutationError.NotFound => NotFound(),
            BenefitMutationError.Validation => BadRequest(new { error = "Validation failed." }),
            BenefitMutationError.InvalidState => BadRequest(new { error = "Invalid state." }),
            _ => BadRequest(),
        };
    }

    private Guid GetUserIdOrThrow()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out var g))
            throw new InvalidOperationException("Authenticated user id is missing.");
        return g;
    }
}
