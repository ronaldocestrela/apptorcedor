using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Payments.Application.Commands.StartStripeConnectOnboarding;
using SocioTorcedor.Modules.Payments.Application.Queries.GetStripeConnectStatus;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/payments/admin/connect")]
public sealed class AdminStripeConnectController(IMediator mediator, ICurrentTenantContext tenantContext) : ControllerBase
{
    [HttpPost("onboarding")]
    public async Task<IActionResult> StartOnboarding([FromBody] OnboardingBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new StartStripeConnectOnboardingCommand(tenantContext.TenantId, body.RefreshUrl, body.ReturnUrl),
            cancellationToken);

        return FromResult(result, r => Ok(r));
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetStripeConnectStatusQuery(tenantContext.TenantId), cancellationToken);
        return FromResult(result, Ok);
    }

    public sealed class OnboardingBody
    {
        public string RefreshUrl { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = string.Empty;
    }

    private IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value!);

        return ProblemResult(result.Error!);
    }

    private IActionResult ProblemResult(Error error)
    {
        if (error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { code = error.Code, message = error.Message });

        if (error.Code.Contains("Conflict", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Exists", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { code = error.Code, message = error.Message });

        if (error.Code.Contains("Auth", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { code = error.Code, message = error.Message });

        return BadRequest(new { code = error.Code, message = error.Message });
    }
}
