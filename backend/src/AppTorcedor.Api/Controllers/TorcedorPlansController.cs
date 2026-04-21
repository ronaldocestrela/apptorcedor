using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetPlanDetails;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListPublishedPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class TorcedorPlansController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
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

    [HttpGet("{planId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<TorcedorPublishedPlanDetailResponse>> GetById(Guid planId, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetPlanDetailsQuery(planId), cancellationToken).ConfigureAwait(false);
        if (dto is null)
            return NotFound();

        var benefits = dto.Benefits
            .Select(b => new TorcedorPublishedPlanDetailBenefitResponse(b.BenefitId, b.SortOrder, b.Title, b.Description))
            .ToList();

        return Ok(
            new TorcedorPublishedPlanDetailResponse(
                dto.PlanId,
                dto.Name,
                dto.Price,
                dto.BillingCycle,
                dto.DiscountPercentage,
                dto.Summary,
                dto.RulesNotes,
                benefits));
    }
}
