using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.StripeWebhooks;
using Stripe;
using Stripe.Checkout;

namespace SocioTorcedor.Modules.Payments.Infrastructure.Services;

/// <summary>
/// Converte notificações thin (V2) em payload no formato snapshot esperado pelos applicators.
/// </summary>
public sealed class StripeThinWebhookPayloadFactory(StripeClient client, ILogger<StripeThinWebhookPayloadFactory> logger)
    : IStripeThinWebhookPayloadFactory
{
    public async Task<StripeThinSyntheticWebhook?> BuildAsync(
        StripeThinWebhookDispatch dispatch,
        string notificationId,
        string notificationType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(notificationId) || string.IsNullOrWhiteSpace(notificationType))
            return null;

        if (string.Equals(notificationType, "v2.core.event_destination.ping", StringComparison.OrdinalIgnoreCase))
            return null;

        Stripe.V2.Core.Event fullEvent;
        try
        {
            fullEvent = await client.V2.Core.Events.GetAsync(notificationId, cancellationToken: cancellationToken);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe thin webhook: failed to retrieve V2 event {EventId}.", notificationId);
            return null;
        }

        var normalized = StripeThinEventTypeNormalizer.Normalize(notificationType);
        if (string.IsNullOrEmpty(normalized))
            return null;

        using var evDoc = JsonDocument.Parse(fullEvent.ToJson());
        var evRoot = evDoc.RootElement;

        var idempotencyKey = ResolveIdempotencyKey(evRoot, notificationId);
        var stripeAccount = dispatch == StripeThinWebhookDispatch.Connect
            ? TryGetConnectedAccountId(evRoot)
            : null;

        if (!evRoot.TryGetProperty("related_object", out var related) ||
            !related.TryGetProperty("id", out var ridEl) ||
            ridEl.ValueKind != JsonValueKind.String)
        {
            logger.LogInformation(
                "Stripe thin webhook: no related_object on event {EventId} (type {Type}).",
                notificationId,
                notificationType);
            return null;
        }

        var relatedId = ridEl.GetString()!;
        var relatedType = related.TryGetProperty("type", out var rt) && rt.ValueKind == JsonValueKind.String
            ? rt.GetString() ?? string.Empty
            : string.Empty;

        var resourceJson = await FetchResourceJsonAsync(relatedId, relatedType, stripeAccount, cancellationToken);
        if (string.IsNullOrWhiteSpace(resourceJson))
            return null;

        var snapshotShaped = BuildSnapshotShapedJson(normalized, resourceJson);
        return new StripeThinSyntheticWebhook(idempotencyKey, normalized, snapshotShaped);
    }

    private static string ResolveIdempotencyKey(JsonElement fullEventRoot, string notificationId)
    {
        if (fullEventRoot.TryGetProperty("snapshot_event", out var se) && se.ValueKind == JsonValueKind.String)
        {
            var s = se.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                return s!;
        }

        return notificationId;
    }

    private static string? TryGetConnectedAccountId(JsonElement fullEventRoot)
    {
        if (!fullEventRoot.TryGetProperty("context", out var ctx) || ctx.ValueKind != JsonValueKind.Object)
            return null;

        if (ctx.TryGetProperty("stripe_account", out var sa) && sa.ValueKind == JsonValueKind.String)
            return sa.GetString();
        if (ctx.TryGetProperty("account", out var a) && a.ValueKind == JsonValueKind.String)
            return a.GetString();

        return null;
    }

    private async Task<string?> FetchResourceJsonAsync(
        string relatedId,
        string relatedType,
        string? stripeAccountId,
        CancellationToken cancellationToken)
    {
        var ro = new RequestOptions();
        if (!string.IsNullOrWhiteSpace(stripeAccountId))
            ro.StripeAccount = stripeAccountId;

        try
        {
            var t = relatedType.ToLowerInvariant();
            if (t.Contains("invoice", StringComparison.Ordinal) || relatedId.StartsWith("in_", StringComparison.Ordinal))
            {
                var inv = await new InvoiceService(client).GetAsync(relatedId, requestOptions: ro, cancellationToken: cancellationToken);
                return inv.ToJson();
            }

            if (t.Contains("subscription", StringComparison.Ordinal) || relatedId.StartsWith("sub_", StringComparison.Ordinal))
            {
                var sub = await new SubscriptionService(client).GetAsync(relatedId, requestOptions: ro, cancellationToken: cancellationToken);
                return sub.ToJson();
            }

            if ((t.Contains("checkout", StringComparison.Ordinal) && t.Contains("session", StringComparison.Ordinal)) ||
                relatedId.StartsWith("cs_", StringComparison.Ordinal))
            {
                var sess = await new SessionService(client).GetAsync(relatedId, requestOptions: ro, cancellationToken: cancellationToken);
                return sess.ToJson();
            }

            if (t.Contains("account", StringComparison.Ordinal) || relatedId.StartsWith("acct_", StringComparison.Ordinal))
            {
                var acc = await new AccountService(client).GetAsync(relatedId, requestOptions: ro, cancellationToken: cancellationToken);
                return acc.ToJson();
            }
        }
        catch (StripeException ex)
        {
            logger.LogWarning(
                ex,
                "Stripe thin webhook: failed to fetch related object {RelatedId} ({RelatedType}).",
                relatedId,
                relatedType);
            return null;
        }

        logger.LogInformation(
            "Stripe thin webhook: unsupported related_object type {RelatedType} id {RelatedId}.",
            relatedType,
            relatedId);
        return null;
    }

    private static string BuildSnapshotShapedJson(string normalizedEventType, string resourceJson)
    {
        var node = new JsonObject
        {
            ["type"] = normalizedEventType,
            ["data"] = new JsonObject
            {
                ["object"] = JsonNode.Parse(resourceJson)!
            }
        };

        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}
