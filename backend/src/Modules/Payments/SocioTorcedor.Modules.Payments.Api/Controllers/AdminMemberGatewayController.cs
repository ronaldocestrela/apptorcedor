using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Payments.Application.Commands.ConfigureTenantMemberStripeDirect;
using SocioTorcedor.Modules.Payments.Application.Queries.GetTenantMemberGatewayStatus;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

/// <summary>
/// Admin do clube: credenciais do gateway escolhido para cobrar sócios.
/// </summary>
[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/payments/admin/member-gateway")]
public sealed class AdminMemberGatewayController(IMediator mediator, ICurrentTenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetTenantMemberGatewayStatusQuery(tenantContext.TenantId),
            cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpPut("stripe-direct")]
    public async Task<IActionResult> ConfigureStripeDirect([FromBody] StripeDirectBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ConfigureTenantMemberStripeDirectCommand(
                tenantContext.TenantId,
                body.SecretKey ?? string.Empty,
                body.PublishableKey,
                body.WebhookSecret),
            cancellationToken);

        return FromResult(result, () => NoContent());
    }

    public sealed class StripeDirectBody
    {
        public string? SecretKey { get; set; }

        public string? PublishableKey { get; set; }

        public string? WebhookSecret { get; set; }
    }

    private IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value!);

        return ProblemResult(result.Error!);
    }

    private IActionResult FromResult(Result result, Func<IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess();

        return ProblemResult(result.Error!);
    }

    private IActionResult ProblemResult(Error error)
    {
        if (error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { code = error.Code, message = error.Message });

        if (error.Code.Contains("Conflict", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Exists", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { code = error.Code, message = error.Message });

        if (error.Code.Contains("Validation", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { code = error.Code, message = error.Message });

        return BadRequest(new { code = error.Code, message = error.Message });
    }
}
