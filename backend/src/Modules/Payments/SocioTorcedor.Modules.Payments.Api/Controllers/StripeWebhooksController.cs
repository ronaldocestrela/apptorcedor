using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessStripeConnectWebhook;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessTenantSaasWebhook;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.StripeWebhooks;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;
using Stripe;
using EventNotification = Stripe.V2.Core.EventNotification;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
public sealed class StripeWebhooksController(
    IMediator mediator,
    IOptions<PaymentsOptions> paymentsOptions,
    StripeClient stripeClient,
    IStripeThinWebhookPayloadFactory thinPayloadFactory) : ControllerBase
{
    private const int WebhookSignatureToleranceSeconds = 300;

    private readonly PaymentsOptions _options = paymentsOptions.Value;

    /// <summary>
    /// Webhook SaaS (thin events / Event Destinations). Requer <c>object: v2.core.event</c> no corpo.
    /// </summary>
    [HttpPost("saas")]
    public async Task<IActionResult> SaaS(CancellationToken cancellationToken)
    {
        var json = await ReadBodyAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var secret = ResolveThinSaasSecret();
        if (string.IsNullOrWhiteSpace(secret))
            return BadRequest(new { error = "Stripe SaaS thin webhook secret is not configured (Payments:StripeThinSaasWebhookSecret or Payments:StripeSaasWebhookSecret)." });

        if (!StripeWebhookEnvelope.IsThinEventNotification(json))
            return BadRequest(new { error = "Expected Stripe thin event (object: v2.core.event). Use an Event Destination with thin events enabled." });

        EventNotification notification;
        try
        {
            notification = stripeClient.ParseEventNotification(json, signature, secret, WebhookSignatureToleranceSeconds);
        }
        catch (StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        if (string.Equals(notification.Type, "v2.core.event_destination.ping", StringComparison.OrdinalIgnoreCase))
            return Ok();

        var built = await thinPayloadFactory.BuildAsync(
            StripeThinWebhookDispatch.SaaS,
            notification.Id,
            notification.Type,
            cancellationToken);

        if (built is null)
            return Ok();

        var result = await mediator.Send(
            new ProcessTenantSaasWebhookCommand(built.IdempotencyKey, built.EventType, built.SnapshotShapedJson),
            cancellationToken);

        return result.IsSuccess ? Ok() : ProblemWebhook(result.Error?.Message ?? "Webhook processing failed.");
    }

    /// <summary>
    /// Webhook Connect (thin events / Event Destinations).
    /// </summary>
    [HttpPost("connect")]
    public async Task<IActionResult> Connect(CancellationToken cancellationToken)
    {
        var json = await ReadBodyAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var secret = ResolveThinConnectSecret();
        if (string.IsNullOrWhiteSpace(secret))
            return BadRequest(new { error = "Stripe Connect thin webhook secret is not configured (Payments:StripeThinConnectWebhookSecret or Payments:StripeConnectWebhookSecret)." });

        if (!StripeWebhookEnvelope.IsThinEventNotification(json))
            return BadRequest(new { error = "Expected Stripe thin event (object: v2.core.event). Use an Event Destination with thin events enabled." });

        EventNotification notification;
        try
        {
            notification = stripeClient.ParseEventNotification(json, signature, secret, WebhookSignatureToleranceSeconds);
        }
        catch (StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        if (string.Equals(notification.Type, "v2.core.event_destination.ping", StringComparison.OrdinalIgnoreCase))
            return Ok();

        var built = await thinPayloadFactory.BuildAsync(
            StripeThinWebhookDispatch.Connect,
            notification.Id,
            notification.Type,
            cancellationToken);

        if (built is null)
            return Ok();

        var result = await mediator.Send(
            new ProcessStripeConnectWebhookCommand(built.IdempotencyKey, built.EventType, built.SnapshotShapedJson),
            cancellationToken);

        return result.IsSuccess ? Ok() : ProblemWebhook(result.Error?.Message ?? "Webhook processing failed.");
    }

    private string ResolveThinSaasSecret() =>
        !string.IsNullOrWhiteSpace(_options.StripeThinSaasWebhookSecret)
            ? _options.StripeThinSaasWebhookSecret
            : _options.StripeSaasWebhookSecret;

    private string ResolveThinConnectSecret() =>
        !string.IsNullOrWhiteSpace(_options.StripeThinConnectWebhookSecret)
            ? _options.StripeThinConnectWebhookSecret
            : _options.StripeConnectWebhookSecret;

    private async Task<string> ReadBodyAsync(CancellationToken cancellationToken)
    {
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;
        return body;
    }

    private IActionResult ProblemWebhook(string message) =>
        BadRequest(new { error = message });
}
