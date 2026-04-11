using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessStripeConnectWebhook;
using SocioTorcedor.Modules.Payments.Application.Commands.ProcessTenantSaasWebhook;
using SocioTorcedor.Modules.Payments.Infrastructure.Options;
using Stripe;

namespace SocioTorcedor.Modules.Payments.Api.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
public sealed class StripeWebhooksController(IMediator mediator, IOptions<PaymentsOptions> paymentsOptions) : ControllerBase
{
    private readonly PaymentsOptions _options = paymentsOptions.Value;

    [HttpPost("saas")]
    public async Task<IActionResult> SaaS(CancellationToken cancellationToken)
    {
        var json = await ReadBodyAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(_options.StripeSaasWebhookSecret))
            return BadRequest(new { error = "Stripe SaaS webhook secret is not configured." });

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, _options.StripeSaasWebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        var result = await mediator.Send(
            new ProcessTenantSaasWebhookCommand(stripeEvent.Id, stripeEvent.Type, json),
            cancellationToken);

        return result.IsSuccess ? Ok() : ProblemWebhook(result.Error?.Message ?? "Webhook processing failed.");
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect(CancellationToken cancellationToken)
    {
        var json = await ReadBodyAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(_options.StripeConnectWebhookSecret))
            return BadRequest(new { error = "Stripe Connect webhook secret is not configured." });

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, _options.StripeConnectWebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        var result = await mediator.Send(
            new ProcessStripeConnectWebhookCommand(stripeEvent.Id, stripeEvent.Type, json),
            cancellationToken);

        return result.IsSuccess ? Ok() : ProblemWebhook(result.Error?.Message ?? "Webhook processing failed.");
    }

    private async Task<string> ReadBodyAsync(CancellationToken cancellationToken)
    {
        // Body buffering for `/api/webhooks/*` is enabled in host middleware before the controller runs.
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;
        return json;
    }

    private IActionResult ProblemWebhook(string message) =>
        BadRequest(new { error = message });
}
