using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberPixCheckout;
using SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberStripeCheckoutSession;
using SocioTorcedor.Modules.Payments.Application.Commands.SubscribeMemberPlan;
using SocioTorcedor.Modules.Payments.Application.Queries.GetMyMemberBilling;
using SocioTorcedor.Modules.Payments.Application.Queries.ListMyMemberInvoices;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

[ApiController]
[Route("api/payments/member")]
public sealed class MemberPaymentsController(IMediator mediator) : ControllerBase
{
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
    [HttpPost("checkout/stripe-session")]
    public async Task<IActionResult> CheckoutStripeSession([FromBody] StripeCheckoutBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateMemberStripeCheckoutSessionCommand(body.MemberPlanId, body.SuccessUrl, body.CancelUrl),
            cancellationToken);

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

    public sealed class SubscribeBody
    {
        public Guid MemberPlanId { get; set; }

        public PaymentMethodKind PaymentMethod { get; set; } = PaymentMethodKind.Unspecified;
    }

    public sealed class PixBody
    {
        public Guid MemberPlanId { get; set; }
    }

    public sealed class StripeCheckoutBody
    {
        public Guid MemberPlanId { get; set; }

        public string SuccessUrl { get; set; } = string.Empty;

        public string CancelUrl { get; set; } = string.Empty;
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
