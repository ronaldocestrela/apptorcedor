using AppTorcedor.Infrastructure.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class AsaasWebhooksController(
    IAsaasWebhookProcessor processor,
    ILogger<AsaasWebhooksController> logger) : ControllerBase
{
    internal const string WebhookResultHeaderName = "X-Asaas-Webhook-Result";

    /// <summary>Webhook ASAAS (token no header <c>asaas-access-token</c>).</summary>
    [HttpPost("asaas")]
    [AllowAnonymous]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var token = Request.Headers["asaas-access-token"].ToString();
        if (string.IsNullOrEmpty(token))
            token = Request.Headers["Asaas-Access-Token"].ToString();

        var result = await processor.ProcessAsync(json, token, cancellationToken).ConfigureAwait(false);
        Response.Headers.Append(WebhookResultHeaderName, result.ToString());

        if (result is AsaasWebhookProcessResult.Unauthorized or AsaasWebhookProcessResult.InvalidPayload)
        {
            logger.LogWarning(
                "ASAAS webhook rejeitado com HTTP 4xx: {Result}.",
                result);
        }
        else if (result is AsaasWebhookProcessResult.ConfigurationError)
        {
            logger.LogError(
                "ASAAS webhook rejeitado: {Result}. Verifique Payments:Asaas:WebhookToken.",
                result);
        }

        return result switch
        {
            AsaasWebhookProcessResult.Ok => Ok(),
            AsaasWebhookProcessResult.IgnoredEventType => Ok(),
            AsaasWebhookProcessResult.Unauthorized => Unauthorized(),
            AsaasWebhookProcessResult.ConfigurationError => StatusCode(StatusCodes.Status500InternalServerError),
            AsaasWebhookProcessResult.InvalidPayload => BadRequest(),
            _ => BadRequest(),
        };
    }
}
