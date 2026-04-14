using System.Security.Claims;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Commands.ConfirmSubscriptionPayment;
using AppTorcedor.Application.Modules.Torcedor.Commands.CreateSubscriptionCheckout;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public sealed class TorcedorSubscriptionsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(TorcedorSubscriptionCheckoutResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Subscribe(
        [FromBody] TorcedorSubscriptionCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrDefault();
        if (userId is null)
            return Unauthorized();

        var result = await mediator
            .Send(
                new CreateTorcedorSubscriptionCheckoutCommand(userId.Value, request.PlanId, request.PaymentMethod),
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Ok)
            return MapSubscribeFailure(result.SubscribeError!.Value);

        var methodStr = result.PaymentMethod == TorcedorSubscriptionPaymentMethod.Pix ? "Pix" : "Card";
        return Ok(
            new TorcedorSubscriptionCheckoutResponse(
                result.MembershipId!.Value,
                result.PaymentId!.Value,
                methodStr,
                result.Amount!.Value,
                result.Currency!,
                result.MembershipStatus!.Value.ToString(),
                result.Pix is { } px
                    ? new TorcedorSubscriptionCheckoutPixResponse(px.QrCodePayload, px.CopyPasteKey)
                    : null,
                result.Card is { } cd ? new TorcedorSubscriptionCheckoutCardResponse(cd.CheckoutUrl) : null));
    }

    [HttpPost("payments/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback(
        [FromBody] TorcedorSubscriptionPaymentCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var r = await mediator
            .Send(new ConfirmTorcedorSubscriptionPaymentCommand(request.PaymentId, request.Secret), cancellationToken)
            .ConfigureAwait(false);
        if (r.Ok)
            return NoContent();

        return r.Error switch
        {
            ConfirmTorcedorSubscriptionPaymentError.NotFound => NotFound(),
            ConfirmTorcedorSubscriptionPaymentError.InvalidState => Conflict(),
            ConfirmTorcedorSubscriptionPaymentError.InvalidWebhookSecret => Unauthorized(),
            _ => BadRequest(),
        };
    }

    private IActionResult MapSubscribeFailure(SubscribeMemberError err) =>
        err switch
        {
            SubscribeMemberError.PlanNotFoundOrNotAvailable => BadRequest(new { error = "plan_not_available" }),
            SubscribeMemberError.AlreadyActiveSubscription => Conflict(new { error = "already_active_subscription" }),
            SubscribeMemberError.SubscriptionPendingPayment => Conflict(new { error = "subscription_pending_payment" }),
            SubscribeMemberError.MembershipStatusPreventsSubscribe => BadRequest(new { error = "membership_status_prevents_subscribe" }),
            SubscribeMemberError.GatewayDoesNotSupportPaymentMethod => BadRequest(new { error = "payment_method_not_supported" }),
            _ => BadRequest(),
        };

    private Guid? GetUserIdOrDefault()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var g) ? g : null;
    }
}
