namespace SocioTorcedor.BuildingBlocks.Application.Payments;

public sealed record CreateSubscriptionRequest(
    PaymentProviderContext Context,
    string TenantOrMemberReference,
    decimal Amount,
    string Currency,
    string BillingInterval,
    string? IdempotencyKey = null,
    string? StripePriceId = null,
    string? ConnectedAccountId = null,
    string? CustomerEmail = null,
    string? ProductName = null,
    IReadOnlyDictionary<string, string>? AdditionalMetadata = null);

public sealed record CreateSubscriptionResult(
    string ExternalCustomerId,
    string ExternalSubscriptionId,
    string Status,
    DateTimeOffset? CurrentPeriodEndUtc = null);

public sealed record CreatePixChargeRequest(
    PaymentProviderContext Context,
    string Reference,
    decimal Amount,
    string Currency,
    string Description,
    string? ConnectedAccountId = null);

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

public sealed record CreateCheckoutSessionRequest(
    PaymentProviderContext Context,
    string Mode,
    decimal Amount,
    string Currency,
    string ProductName,
    string BillingInterval,
    string SuccessUrl,
    string CancelUrl,
    IReadOnlyDictionary<string, string> Metadata,
    string? ConnectedAccountId,
    string? CustomerEmail,
    string? IdempotencyKey);

public sealed record CreateCheckoutSessionResult(string SessionId, string Url);

public sealed record CreateBillingPortalSessionRequest(
    string CustomerId,
    string ReturnUrl,
    string? IdempotencyKey);

public sealed record CreateBillingPortalSessionResult(string Url);

/// <summary>Item de cartão salvo no customer Stripe (conta plataforma, Billing SaaS).</summary>
public sealed record SaasPaymentMethodListItem(
    string Id,
    string Brand,
    string Last4,
    int ExpMonth,
    int ExpYear,
    bool IsDefault);

public sealed record ListSaasCustomerPaymentMethodsRequest(string CustomerId);

public sealed record ListSaasCustomerPaymentMethodsResult(IReadOnlyList<SaasPaymentMethodListItem> Items);

public sealed record CreateSaasSetupIntentRequest(string CustomerId, string? IdempotencyKey);

public sealed record CreateSaasSetupIntentResult(string ClientSecret, string SetupIntentId);

public sealed record AttachSaasPaymentMethodRequest(
    string CustomerId,
    string PaymentMethodId,
    bool SetAsDefault,
    string? ExternalSubscriptionId,
    string? IdempotencyKey);

public sealed record DetachSaasPaymentMethodRequest(string CustomerId, string PaymentMethodId);
