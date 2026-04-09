using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.Modules.Backoffice.Application.Commands.AssignPlanToTenant;
using SocioTorcedor.Modules.Backoffice.Application.Commands.RevokeTenantPlan;
using SocioTorcedor.Modules.Backoffice.Application.Queries.GetTenantPlan;
using SocioTorcedor.Modules.Backoffice.Application.Queries.ListTenantsByPlan;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;

namespace SocioTorcedor.Modules.Backoffice.Api.Controllers;

[ApiController]
[Route("api/backoffice/tenant-plans")]
public sealed class TenantPlansController(IMediator mediator) : BackofficeControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AssignPlanToTenantCommand(
                body.TenantId,
                body.SaaSPlanId,
                body.StartDate,
                body.EndDate,
                body.BillingCycle),
            cancellationToken);

        return FromResult(result, id => CreatedAtAction(nameof(GetByTenant), new { tenantId = body.TenantId }, new { id }));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RevokeTenantPlanCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTenantPlanQuery(tenantId), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet("plan/{planId:guid}")]
    public async Task<IActionResult> ListByPlan(
        Guid planId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListTenantsByPlanQuery(planId, page, pageSize), cancellationToken);
        return FromResult(result, Ok);
    }

    public sealed class AssignBody
    {
        public Guid TenantId { get; set; }

        public Guid SaaSPlanId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public BillingCycle BillingCycle { get; set; }
    }
}
