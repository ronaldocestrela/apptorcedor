namespace SocioTorcedor.BuildingBlocks.Application.Payments;

public sealed record CreateSubscriptionRequest(
    PaymentProviderContext Context,
    string TenantOrMemberReference,
    decimal Amount,
    string Currency,
    string BillingInterval);

public sealed record CreateSubscriptionResult(
    string ExternalCustomerId,
    string ExternalSubscriptionId,
    string Status);

public sealed record CreatePixChargeRequest(
    PaymentProviderContext Context,
    string Reference,
    decimal Amount,
    string Currency,
    string Description);

public sealed record CreatePixChargeResult(
    string ExternalChargeId,
    string? PixCopyPaste,
    string? QrCodePayload,
    DateTimeOffset? ExpiresAt);

public sealed record CreateCardChargeRequest(
    PaymentProviderContext Context,
    string Reference,
    decimal Amount,
    string Currency,
    string CardTokenOrPaymentMethodId);

public sealed record CreateCardChargeResult(
    string ExternalChargeId,
    string Status);

public sealed record PaymentProviderStatusResult(
    string ExternalId,
    string Status,
    string? Raw);

public sealed record PaymentProviderWebhookEnvelope(
    PaymentProviderContext Context,
    string EventType,
    string RawBody,
    IReadOnlyDictionary<string, string> Headers);
