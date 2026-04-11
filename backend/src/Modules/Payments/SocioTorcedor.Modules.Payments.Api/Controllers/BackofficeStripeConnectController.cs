using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.Modules.Backoffice.Api.Controllers;
using SocioTorcedor.Modules.Payments.Application.Commands.StartStripeConnectOnboarding;
using SocioTorcedor.Modules.Payments.Application.Queries.GetStripeConnectStatus;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

[ApiController]
[Route("api/backoffice/payments/connect")]
public sealed class BackofficeStripeConnectController(IMediator mediator) : BackofficeControllerBase
{
    [HttpPost("tenants/{tenantId:guid}/onboarding")]
    public async Task<IActionResult> StartOnboarding(Guid tenantId, [FromBody] OnboardingBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new StartStripeConnectOnboardingCommand(tenantId, body.RefreshUrl, body.ReturnUrl),
            cancellationToken);

        return FromResult(result, r => Ok(r));
    }

    [HttpGet("tenants/{tenantId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStripeConnectStatusQuery(tenantId), cancellationToken);
        return FromResult(result, Ok);
    }

    public sealed class OnboardingBody
    {
        public string RefreshUrl { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = string.Empty;
    }
}
