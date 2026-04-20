using System.Security.Claims;
using AppTorcedor.Api.Contracts;
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
                i.EndAt))
            .ToList();
        return Ok(new TorcedorEligibleBenefitOffersPageResponse(pageDto.TotalCount, items));
    }

    private Guid? GetUserIdOrDefault()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var g) ? g : null;
    }
}
