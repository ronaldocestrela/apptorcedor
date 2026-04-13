using AppTorcedor.Identity;

namespace AppTorcedor.Application.Abstractions;

public enum TorcedorSubscriptionPaymentMethod
{
    Pix = 0,
    Card = 1,
}

public sealed record TorcedorSubscriptionCheckoutPixDto(string QrCodePayload, string? CopyPasteKey);

public sealed record TorcedorSubscriptionCheckoutCardDto(string CheckoutUrl);

public sealed record CreateTorcedorSubscriptionCheckoutResult(
    bool Ok,
    SubscribeMemberError? SubscribeError,
    Guid? MembershipId,
    Guid? PaymentId,
    decimal? Amount,
    string? Currency,
    TorcedorSubscriptionPaymentMethod? PaymentMethod,
    MembershipStatus? MembershipStatus,
    TorcedorSubscriptionCheckoutPixDto? Pix,
    TorcedorSubscriptionCheckoutCardDto? Card)
{
    public static CreateTorcedorSubscriptionCheckoutResult Success(
        Guid membershipId,
        Guid paymentId,
        decimal amount,
        string currency,
        TorcedorSubscriptionPaymentMethod method,
        MembershipStatus membershipStatus,
        TorcedorSubscriptionCheckoutPixDto? pix,
        TorcedorSubscriptionCheckoutCardDto? card) =>
        new(
            true,
            null,
            membershipId,
            paymentId,
            amount,
            currency,
            method,
            membershipStatus,
            pix,
            card);

    public static CreateTorcedorSubscriptionCheckoutResult SubscribeFailed(SubscribeMemberError error) =>
        new(false, error, null, null, null, null, null, null, null, null);
}

public enum ConfirmTorcedorSubscriptionPaymentError
{
    NotFound,
    InvalidState,
    InvalidWebhookSecret,
}

public sealed record ConfirmTorcedorSubscriptionPaymentResult(bool Ok, ConfirmTorcedorSubscriptionPaymentError? Error)
{
    public static ConfirmTorcedorSubscriptionPaymentResult Success() => new(true, null);

    public static ConfirmTorcedorSubscriptionPaymentResult Failure(ConfirmTorcedorSubscriptionPaymentError error) =>
        new(false, error);
}

/// <summary>Contratação com cobrança inicial (Parte D.4).</summary>
public interface ITorcedorSubscriptionCheckoutPort
{
    Task<CreateTorcedorSubscriptionCheckoutResult> CreateCheckoutAsync(
        Guid userId,
        Guid planId,
        TorcedorSubscriptionPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default);

    Task<ConfirmTorcedorSubscriptionPaymentResult> ConfirmPaymentAsync(
        Guid paymentId,
        string? webhookSecret,
        CancellationToken cancellationToken = default);
}
