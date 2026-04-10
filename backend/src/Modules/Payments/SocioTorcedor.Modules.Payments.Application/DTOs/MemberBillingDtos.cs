using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.DTOs;

public sealed record MemberBillingSubscriptionDto(
    Guid Id,
    Guid MemberProfileId,
    Guid MemberPlanId,
    decimal RecurringAmount,
    string Currency,
    PaymentMethodKind PaymentMethod,
    BillingSubscriptionStatus Status,
    string? ExternalCustomerId,
    string? ExternalSubscriptionId,
    DateTime? NextBillingAtUtc,
    DateTime CreatedAtUtc);

public sealed record MemberBillingInvoiceDto(
    Guid Id,
    Guid MemberBillingSubscriptionId,
    decimal Amount,
    string Currency,
    PaymentMethodKind PaymentMethod,
    DateTime DueAtUtc,
    BillingInvoiceStatus Status,
    string? ExternalInvoiceId,
    string? PixCopyPaste,
    DateTime? PaidAtUtc,
    DateTime CreatedAtUtc);

public sealed record MemberPixCheckoutDto(
    Guid InvoiceId,
    string? ExternalChargeId,
    string? PixCopyPaste,
    DateTime? ExpiresAtUtc);
