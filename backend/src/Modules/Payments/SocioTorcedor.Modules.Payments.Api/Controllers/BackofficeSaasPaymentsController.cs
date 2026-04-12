using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.Modules.Backoffice.Api.Controllers;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateTenantSaasBillingPortalSession;
using SocioTorcedor.Modules.Payments.Application.Commands.StartTenantSaasBilling;
using SocioTorcedor.Modules.Payments.Application.Queries.GetTenantSaasBilling;
using SocioTorcedor.Modules.Payments.Application.Queries.ListTenantSaasInvoices;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

[ApiController]
[Route("api/backoffice/payments/saas")]
public sealed class BackofficeSaasPaymentsController(IMediator mediator) : BackofficeControllerBase
{
    [HttpPost("tenants/{tenantId:guid}/billing/start")]
    public async Task<IActionResult> StartBilling(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new StartTenantSaasBillingCommand(tenantId), cancellationToken);
        return FromResult(result, id => CreatedAtAction(nameof(GetSubscription), new { tenantId }, new { id }));
    }

    [HttpGet("tenants/{tenantId:guid}/subscription")]
    public async Task<IActionResult> GetSubscription(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTenantSaasBillingQuery(tenantId), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet("tenants/{tenantId:guid}/invoices")]
    public async Task<IActionResult> ListInvoices(
        Guid tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListTenantSaasInvoicesQuery(tenantId, page, pageSize), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost("tenants/{tenantId:guid}/billing/portal")]
    public async Task<IActionResult> CreatePortalSession(
        Guid tenantId,
        [FromBody] PortalBody body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateTenantSaasBillingPortalSessionCommand(tenantId, body.ReturnUrl),
            cancellationToken);

        return FromResult(result, Ok);
    }

    public sealed class PortalBody
    {
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
