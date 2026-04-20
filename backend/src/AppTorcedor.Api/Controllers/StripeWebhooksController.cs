using AppTorcedor.Infrastructure.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class StripeWebhooksController(
    IStripeWebhookProcessor processor,
    ILogger<StripeWebhooksController> logger) : ControllerBase
{
    internal const string WebhookResultHeaderName = "X-Stripe-Webhook-Result";

    /// <summary>Stripe webhook (assinatura HMAC via <c>Stripe-Signature</c>).</summary>
    [HttpPost("stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var result = await processor.ProcessAsync(json, signature, cancellationToken).ConfigureAwait(false);

        Response.Headers.Append(WebhookResultHeaderName, result.ToString());

        if (result is StripeWebhookProcessResult.BadSignature or StripeWebhookProcessResult.InvalidPayload)
        {
            logger.LogWarning(
                "Stripe webhook rejected with HTTP 4xx: {StripeWebhookResult}. See processor logs for detail.",
                result);
        }
        else if (result is StripeWebhookProcessResult.ConfigurationError)
        {
            logger.LogError(
                "Stripe webhook rejected: {StripeWebhookResult}. Check Payments:Stripe:WebhookSecret.",
                result);
        }

        return result switch
        {
            StripeWebhookProcessResult.Ok => Ok(),
            StripeWebhookProcessResult.IgnoredEventType => Ok(),
            StripeWebhookProcessResult.BadSignature => BadRequest(),
            StripeWebhookProcessResult.ConfigurationError => StatusCode(StatusCodes.Status500InternalServerError),
            StripeWebhookProcessResult.InvalidPayload => BadRequest(),
            _ => BadRequest(),
        };
    }
}
