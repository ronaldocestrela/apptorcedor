using SocioTorcedor.Modules.Backoffice.Domain.Enums;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.DTOs;

public sealed record TenantSaasBillingSubscriptionDto(
    Guid Id,
    Guid TenantId,
    Guid TenantPlanId,
    Guid SaaSPlanId,
    BillingCycle BillingCycle,
    decimal RecurringAmount,
    string Currency,
    BillingSubscriptionStatus Status,
    string? ExternalCustomerId,
    string? ExternalSubscriptionId,
    DateTime? NextBillingAtUtc,
    DateTime CreatedAtUtc);

public sealed record TenantSaasBillingInvoiceDto(
    Guid Id,
    Guid TenantBillingSubscriptionId,
    decimal Amount,
    string Currency,
    DateTime DueAtUtc,
    BillingInvoiceStatus Status,
    string? ExternalInvoiceId,
    DateTime? PaidAtUtc,
    DateTime CreatedAtUtc);

public sealed record TenantSaasPaymentMethodDto(
    string Id,
    string Brand,
    string Last4,
    int ExpMonth,
    int ExpYear,
    bool IsDefault);

public sealed record TenantSaasSetupIntentDto(string ClientSecret, string SetupIntentId);

public sealed record TenantSaasStripeConfigDto(string? PublishableKey);
