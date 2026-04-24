using System.Globalization;
using System.Text.Json;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Payments;

public enum AsaasWebhookProcessResult
{
    Ok,
    Unauthorized,
    ConfigurationError,
    IgnoredEventType,
    InvalidPayload,
}

public interface IAsaasWebhookProcessor
{
    Task<AsaasWebhookProcessResult> ProcessAsync(
        string json,
        string? accessTokenHeader,
        CancellationToken cancellationToken = default);
}

public sealed class AsaasWebhookProcessor(
    AppDbContext db,
    ITorcedorSubscriptionCheckoutPort checkout,
    IOptions<PaymentsOptions> options,
    ILogger<AsaasWebhookProcessor> logger) : IAsaasWebhookProcessor
{
    public async Task<AsaasWebhookProcessResult> ProcessAsync(
        string json,
        string? accessTokenHeader,
        CancellationToken cancellationToken = default)
    {
        var expected = options.Value.Asaas.WebhookToken?.Trim();
        if (string.IsNullOrEmpty(expected))
        {
            logger.LogWarning("ASAAS webhook recebido mas Payments:Asaas:WebhookToken está vazio.");
            return AsaasWebhookProcessResult.ConfigurationError;
        }

        if (!string.Equals(accessTokenHeader?.Trim(), expected, StringComparison.Ordinal))
        {
            logger.LogWarning("ASAAS webhook com asaas-access-token inválido ou ausente.");
            return AsaasWebhookProcessResult.Unauthorized;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "ASAAS webhook JSON inválido.");
            return AsaasWebhookProcessResult.InvalidPayload;
        }

        using (doc)
        {
            var root = doc.RootElement;
            var eventType = root.TryGetProperty("event", out var ev) ? ev.GetString() : null;
            if (string.IsNullOrEmpty(eventType))
            {
                logger.LogWarning("ASAAS webhook sem campo event.");
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            if (!string.Equals(eventType, "PAYMENT_RECEIVED", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(eventType, "PAYMENT_CONFIRMED", StringComparison.OrdinalIgnoreCase))
                return AsaasWebhookProcessResult.IgnoredEventType;

            if (!root.TryGetProperty("payment", out var payment) || payment.ValueKind != JsonValueKind.Object)
            {
                logger.LogWarning("ASAAS webhook sem objeto payment.");
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            var paymentIdStr = payment.TryGetProperty("externalReference", out var er) ? er.GetString() : null;
            if (string.IsNullOrEmpty(paymentIdStr) || !Guid.TryParse(paymentIdStr, out var paymentId))
            {
                logger.LogWarning("ASAAS payment sem externalReference (payment_id) válido.");
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            var asaasPayId = payment.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(asaasPayId))
            {
                logger.LogWarning("ASAAS payment sem id.");
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            var webhookEventId = root.TryGetProperty("id", out var wid) && wid.ValueKind == JsonValueKind.String
                ? wid.GetString()
                : null;
            webhookEventId = string.IsNullOrEmpty(webhookEventId) ? $"{asaasPayId}:{eventType}" : webhookEventId;
            if (webhookEventId.Length > 255)
                webhookEventId = webhookEventId[..255];

            if (await db.ProcessedWebhookEvents.AsNoTracking()
                    .AnyAsync(
                        e => e.Provider == "Asaas" && e.EventId == webhookEventId,
                        cancellationToken)
                    .ConfigureAwait(false))
                return AsaasWebhookProcessResult.Ok;

            var record = await db.Payments.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
                .ConfigureAwait(false);
            if (record is null)
            {
                logger.LogWarning("ASAAS webhook: payment_id {PaymentId} não encontrado.", paymentId);
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            if (!string.Equals(record.ProviderName, "Asaas", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "ASAAS webhook para pagamento {PaymentId} mas ProviderName é {Provider}.",
                    paymentId,
                    record.ProviderName);
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            if (!payment.TryGetProperty("value", out var valEl))
            {
                logger.LogWarning("ASAAS payment sem value.");
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            decimal asaasValue;
            if (valEl.ValueKind == JsonValueKind.Number && valEl.TryGetDecimal(out var d))
                asaasValue = d;
            else if (valEl.ValueKind == JsonValueKind.String && decimal.TryParse(valEl.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                asaasValue = parsed;
            else
            {
                logger.LogWarning("ASAAS payment value inválido.");
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            var expectedAmount = Math.Round(record.Amount, 2, MidpointRounding.AwayFromZero);
            if (!AsaasAmountMatchesCharge(asaasValue, expectedAmount, payment))
            {
                logger.LogWarning(
                    "ASAAS valor divergente para payment {PaymentId}: esperado total {Expected}, recebido {Actual}.",
                    paymentId,
                    expectedAmount,
                    asaasValue);
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            var confirm = await checkout
                .ConfirmPaymentAfterProviderSuccessAsync(paymentId, asaasPayId, cancellationToken)
                .ConfigureAwait(false);
            if (!confirm.Ok)
            {
                logger.LogWarning(
                    "ConfirmPaymentAfterProviderSuccess falhou para {PaymentId}: {Error}.",
                    paymentId,
                    confirm.Error);
                return AsaasWebhookProcessResult.InvalidPayload;
            }

            try
            {
                db.ProcessedWebhookEvents.Add(
                    new ProcessedWebhookEventRecord
                    {
                        Provider = "Asaas",
                        EventId = webhookEventId,
                        EventType = eventType,
                        ProcessedAtUtc = DateTimeOffset.UtcNow,
                        RelatedPaymentId = paymentId,
                    });
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException ex)
            {
                if (await db.ProcessedWebhookEvents.AsNoTracking()
                        .AnyAsync(e => e.Provider == "Asaas" && e.EventId == webhookEventId, cancellationToken)
                        .ConfigureAwait(false))
                    return AsaasWebhookProcessResult.Ok;

                logger.LogError(ex, "Falha ao gravar idempotência ASAAS para {EventId}.", webhookEventId);
                throw;
            }

            return AsaasWebhookProcessResult.Ok;
        }
    }

    /// <summary>Valor no webhook pode ser o total (1x) ou o valor de uma parcela (cartão parcelado).</summary>
    private static bool AsaasAmountMatchesCharge(decimal reported, decimal expectedTotal, JsonElement payment)
    {
        if (Math.Abs(reported - expectedTotal) <= 0.02m)
            return true;

        if (!payment.TryGetProperty("installmentCount", out var icEl) || icEl.ValueKind != JsonValueKind.Number)
            return false;
        if (!icEl.TryGetInt32(out var n) || n < 2)
            return false;

        var per = Math.Round(expectedTotal / n, 2, MidpointRounding.AwayFromZero);
        var remainder = Math.Round(expectedTotal - per * (n - 1), 2, MidpointRounding.AwayFromZero);

        var installmentNumber = payment.TryGetProperty("installmentNumber", out var inEl)
            && inEl.ValueKind == JsonValueKind.Number
            && inEl.TryGetInt32(out var inum)
            ? inum
            : (int?)null;

        if (installmentNumber == n)
            return Math.Abs(reported - remainder) <= 0.06m;

        return Math.Abs(reported - per) <= 0.06m;
    }
}
