using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberPixCheckout;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessMemberTenantWebhook;
using SocioTorcedor.Modules.Payments.Application.Commands.SubscribeMemberPlan;
using SocioTorcedor.Modules.Payments.Application.Queries.GetMyMemberBilling;
using SocioTorcedor.Modules.Payments.Application.Queries.ListMyMemberInvoices;
using SocioTorcedor.Modules.Payments.Domain.Enums;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

[ApiController]
[Route("api/payments/member")]
public sealed class MemberPaymentsController(IMediator mediator, IOptions<PaymentsOptions> paymentsOptions) : ControllerBase
{
    private readonly PaymentsOptions _paymentsOptions = paymentsOptions.Value;

    [Authorize]
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SubscribeMemberPlanCommand(body.MemberPlanId, body.PaymentMethod),
            cancellationToken);

        return FromResult(result, id => CreatedAtAction(nameof(GetMySubscription), null, new { id }));
    }

    [Authorize]
    [HttpPost("checkout/pix")]
    public async Task<IActionResult> CheckoutPix([FromBody] PixBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateMemberPixCheckoutCommand(body.MemberPlanId), cancellationToken);
        return FromResult(result, Ok);
    }

    [Authorize]
    [HttpGet("me/subscription")]
    public async Task<IActionResult> GetMySubscription(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMyMemberBillingQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [Authorize]
    [HttpGet("me/invoices")]
    public async Task<IActionResult> ListMyInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListMyMemberInvoicesQuery(page, pageSize), cancellationToken);
        return FromResult(result, Ok);
    }

    [AllowAnonymous]
    [HttpPost("webhooks")]
    public async Task<IActionResult> Webhook([FromBody] MemberWebhookBody body, CancellationToken cancellationToken)
    {
        var secret = Request.Headers["X-Payments-Webhook-Secret"].ToString().Trim();
        var expected = _paymentsOptions.MemberWebhookSecret?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(expected) || !string.Equals(secret, expected, StringComparison.Ordinal))
            return Unauthorized(new { error = "Invalid webhook secret." });

        if (string.IsNullOrWhiteSpace(body.IdempotencyKey))
            return BadRequest(new { error = "idempotencyKey is required." });

        var raw = string.IsNullOrWhiteSpace(body.RawBody) ? System.Text.Json.JsonSerializer.Serialize(body) : body.RawBody!;
        var result = await mediator.Send(
            new ProcessMemberTenantWebhookCommand(body.IdempotencyKey.Trim(), body.EventType ?? string.Empty, raw),
            cancellationToken);

        return FromResult(result);
    }

    public sealed class SubscribeBody
    {
        public Guid MemberPlanId { get; set; }

        public PaymentMethodKind PaymentMethod { get; set; } = PaymentMethodKind.Unspecified;
    }

    public sealed class PixBody
    {
        public Guid MemberPlanId { get; set; }
    }

    public sealed class MemberWebhookBody
    {
        public string? IdempotencyKey { get; set; }

        public string? EventType { get; set; }

        public string? RawBody { get; set; }

        public string? ExternalSubscriptionId { get; set; }

        public string? MemberProfileId { get; set; }
    }

    private IActionResult FromResult(Result result)
    {
        if (result.IsSuccess)
            return Ok();

        return ProblemResult(result.Error!);
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
