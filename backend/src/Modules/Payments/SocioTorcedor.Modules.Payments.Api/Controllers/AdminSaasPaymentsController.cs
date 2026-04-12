using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Payments.Application.Commands.AttachTenantSaasPaymentMethod;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateTenantSaasSetupIntent;
using SocioTorcedor.Modules.Payments.Application.Commands.DetachTenantSaasPaymentMethod;
using SocioTorcedor.Modules.Payments.Application.DTOs;
using SocioTorcedor.Modules.Payments.Application.Queries.GetTenantSaasBilling;
using SocioTorcedor.Modules.Payments.Application.Queries.ListTenantSaasInvoices;
using SocioTorcedor.Modules.Payments.Application.Queries.ListTenantSaasPaymentMethods;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

/// <summary>
/// Cobrança SaaS do tenant (clube paga a plataforma): assinatura, faturas e cartões no customer Stripe da conta principal.
/// </summary>
[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/payments/admin/saas")]
public sealed class AdminSaasPaymentsController(
    IMediator mediator,
    ICurrentTenantContext tenantContext,
    IOptions<PaymentsOptions> paymentsOptions) : ControllerBase
{
    [HttpGet("stripe-config")]
    public ActionResult<TenantSaasStripeConfigDto> GetStripeConfig()
    {
        var pk = paymentsOptions.Value.StripePublishableKey?.Trim();
        if (string.IsNullOrWhiteSpace(pk))
            return Ok(new TenantSaasStripeConfigDto(null));

        return Ok(new TenantSaasStripeConfigDto(pk));
    }

    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetTenantSaasBillingQuery(tenantContext.TenantId),
            cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> ListInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListTenantSaasInvoicesQuery(tenantContext.TenantId, page, pageSize),
            cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpGet("cards")]
    public async Task<IActionResult> ListCards(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListTenantSaasPaymentMethodsQuery(tenantContext.TenantId),
            cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpPost("cards/setup-intent")]
    public async Task<IActionResult> CreateSetupIntent(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateTenantSaasSetupIntentCommand(tenantContext.TenantId),
            cancellationToken);

        return FromResult(result, Ok);
    }

    [HttpPost("cards")]
    public async Task<IActionResult> AttachCard(
        [FromBody] AttachCardBody body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AttachTenantSaasPaymentMethodCommand(
                tenantContext.TenantId,
                body.PaymentMethodId ?? string.Empty,
                body.SetAsDefault),
            cancellationToken);

        return FromResult(result, () => Ok());
    }

    [HttpDelete("cards/{paymentMethodId}")]
    public async Task<IActionResult> DetachCard(
        string paymentMethodId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DetachTenantSaasPaymentMethodCommand(tenantContext.TenantId, paymentMethodId),
            cancellationToken);

        return FromResult(result, () => NoContent());
    }

    public sealed class AttachCardBody
    {
        public string? PaymentMethodId { get; set; }

        public bool SetAsDefault { get; set; } = true;
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
