using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreatePlan;
using AppTorcedor.Application.Modules.Administration.Commands.UpdatePlan;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminPlanDetail;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminPlans;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/plans")]
[Authorize]
public sealed class AdminPlansController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.PlanosVisualizar)]
    public async Task<ActionResult<AdminPlanListPageDto>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isPublished,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new ListAdminPlansQuery(search, isActive, isPublished, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(pageDto);
    }

    [HttpGet("{planId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.PlanosVisualizar)]
    public async Task<ActionResult<AdminPlanDetailDto>> GetById(Guid planId, CancellationToken cancellationToken)
    {
        var detail = await mediator.Send(new GetAdminPlanDetailQuery(planId), cancellationToken).ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.PlanosCriar)]
    public async Task<ActionResult<object>> Create([FromBody] UpsertPlanRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var dto = Map(body);
        var result = await mediator.Send(new CreatePlanCommand(dto), cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(nameof(GetById), new { planId = result.PlanId }, new { planId = result.PlanId });
    }

    [HttpPut("{planId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.PlanosEditar)]
    public async Task<IActionResult> Update(Guid planId, [FromBody] UpsertPlanRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var dto = Map(body);
        var result = await mediator.Send(new UpdatePlanCommand(planId, dto), cancellationToken).ConfigureAwait(false);
        if (result.ValidationError is { } err)
            return BadRequest(new { error = err });
        if (result.NotFound)
            return NotFound();

        return NoContent();
    }

    private static AdminPlanWriteDto Map(UpsertPlanRequest body) =>
        new(
            body.Name,
            body.Price,
            body.BillingCycle,
            body.DiscountPercentage,
            body.IsActive,
            body.IsPublished,
            body.Summary,
            body.RulesNotes,
            body.Benefits
                .OrderBy(b => b.SortOrder)
                .Select(b => new AdminPlanBenefitInputDto(b.SortOrder, b.Title, b.Description))
                .ToList());
}
