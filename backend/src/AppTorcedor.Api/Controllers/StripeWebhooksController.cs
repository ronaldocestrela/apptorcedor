using AppTorcedor.Infrastructure.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class StripeWebhooksController(IStripeWebhookProcessor processor) : ControllerBase
{
    /// <summary>Stripe webhook (assinatura HMAC via <c>Stripe-Signature</c>).</summary>
    [HttpPost("stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var result = await processor.ProcessAsync(json, signature, cancellationToken).ConfigureAwait(false);
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
