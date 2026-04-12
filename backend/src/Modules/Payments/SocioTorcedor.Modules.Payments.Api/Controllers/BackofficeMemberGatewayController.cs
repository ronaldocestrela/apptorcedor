using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.Modules.Backoffice.Api.Controllers;
using SocioTorcedor.Modules.Payments.Application.Commands.SetTenantMemberPaymentProvider;
using SocioTorcedor.Modules.Payments.Application.Queries.GetTenantMemberGatewayStatus;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

/// <summary>
/// Backoffice: define qual provedor de gateway o tenant usa para cobrar sócios.
/// </summary>
[ApiController]
[Route("api/backoffice/payments/member-gateway")]
public sealed class BackofficeMemberGatewayController(IMediator mediator) : BackofficeControllerBase
{
    [HttpGet("tenants/{tenantId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTenantMemberGatewayStatusQuery(tenantId), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut("tenants/{tenantId:guid}/provider")]
    public async Task<IActionResult> SetProvider(Guid tenantId, [FromBody] SetProviderBody body, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MemberPaymentProviderKind>(body.Provider, ignoreCase: true, out var provider))
            return BadRequest(new { error = "Invalid provider. Use None, StripeDirect, Asaas, or MercadoPago." });

        var result = await mediator.Send(
            new SetTenantMemberPaymentProviderCommand(tenantId, provider),
            cancellationToken);

        return FromResult(result);
    }

    public sealed class SetProviderBody
    {
        /// <summary>Ex.: StripeDirect, None, Asaas, MercadoPago</summary>
        public string Provider { get; set; } = string.Empty;
    }
}
