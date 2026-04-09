using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.Modules.Backoffice.Application.Commands.CreateSaaSPlan;
using SocioTorcedor.Modules.Backoffice.Application.Commands.ToggleSaaSPlan;
using SocioTorcedor.Modules.Backoffice.Application.Commands.UpdateSaaSPlan;
using SocioTorcedor.Modules.Backoffice.Application.Queries.GetSaaSPlanById;
using SocioTorcedor.Modules.Backoffice.Application.Queries.ListSaaSPlans;

namespace SocioTorcedor.Modules.Backoffice.Api.Controllers;

[ApiController]
[Route("api/backoffice/plans")]
public sealed class SaaSPlansController(IMediator mediator) : BackofficeControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanBody body, CancellationToken cancellationToken)
    {
        var features = body.Features?.Select(f => new SaaSPlanFeatureInput(f.Key, f.Description, f.Value)).ToList();

        var result = await mediator.Send(
            new CreateSaaSPlanCommand(
                body.Name,
                body.Description,
                body.MonthlyPrice,
                body.YearlyPrice,
                body.MaxMembers,
                features),
            cancellationToken);

        return FromResult(result, id => CreatedAtAction(nameof(GetById), new { id }, new { id }));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListSaaSPlansQuery(page, pageSize), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSaaSPlanByIdQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanBody body, CancellationToken cancellationToken)
    {
        var features = body.Features?.Select(f => new SaaSPlanFeatureInput(f.Key, f.Description, f.Value)).ToList();

        var result = await mediator.Send(
            new UpdateSaaSPlanCommand(
                id,
                body.Name,
                body.Description,
                body.MonthlyPrice,
                body.YearlyPrice,
                body.MaxMembers,
                features),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ToggleSaaSPlanCommand(id), cancellationToken);
        return FromResult(result);
    }

    public sealed class CreatePlanBody
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal MonthlyPrice { get; set; }

        public decimal? YearlyPrice { get; set; }

        public int MaxMembers { get; set; }

        public List<FeatureBody>? Features { get; set; }
    }

    public sealed class UpdatePlanBody
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal MonthlyPrice { get; set; }

        public decimal? YearlyPrice { get; set; }

        public int MaxMembers { get; set; }

        public List<FeatureBody>? Features { get; set; }
    }

    public sealed class FeatureBody
    {
        public string Key { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Value { get; set; }
    }
}
