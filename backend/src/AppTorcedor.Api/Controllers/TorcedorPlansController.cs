using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListPublishedPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/plans")]
[Authorize]
public sealed class TorcedorPlansController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TorcedorPublishedPlansCatalogResponse>> List(CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new ListPublishedPlansQuery(), cancellationToken).ConfigureAwait(false);
        var items = dto.Items
            .Select(p => new TorcedorPublishedPlanItemResponse(
                p.PlanId,
                p.Name,
                p.Price,
                p.BillingCycle,
                p.DiscountPercentage,
                p.Summary,
                p.Benefits
                    .Select(b => new TorcedorPublishedPlanBenefitResponse(b.BenefitId, b.Title, b.Description))
                    .ToList()))
            .ToList();
        return Ok(new TorcedorPublishedPlansCatalogResponse(items));
    }
}
