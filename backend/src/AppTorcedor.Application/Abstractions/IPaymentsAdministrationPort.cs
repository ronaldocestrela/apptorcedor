namespace AppTorcedor.Application.Abstractions;

public interface IPaymentsAdministrationPort
{
    Task<AdminPaymentListPageDto> ListPaymentsAsync(
        string? status,
        Guid? userId,
        Guid? membershipId,
        string? paymentMethod,
        DateTimeOffset? dueFrom,
        DateTimeOffset? dueTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminPaymentDetailDto?> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<PaymentMutationResult> ConciliatePaymentAsync(
        Guid paymentId,
        DateTimeOffset? paidAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<PaymentMutationResult> CancelPaymentAsync(
        Guid paymentId,
        string? reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<PaymentMutationResult> RefundPaymentAsync(
        Guid paymentId,
        string? reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record AdminPaymentListItemDto(
    Guid PaymentId,
    Guid UserId,
    string UserEmail,
    string UserName,
    Guid MembershipId,
    decimal Amount,
    string Status,
    DateTimeOffset DueDate,
    DateTimeOffset? PaidAt,
    string? PaymentMethod,
    string? ExternalReference);

public sealed record AdminPaymentListPageDto(int TotalCount, IReadOnlyList<AdminPaymentListItemDto> Items);

public sealed record AdminPaymentDetailDto(
    Guid PaymentId,
    Guid UserId,
    string UserEmail,
    string UserName,
    Guid MembershipId,
    decimal Amount,
    string Status,
    DateTimeOffset DueDate,
    DateTimeOffset? PaidAt,
    string? PaymentMethod,
    string? ExternalReference,
    string? ProviderName,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? RefundedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastProviderSyncAt,
    string? StatusReason);

public sealed record PaymentMutationResult(bool Ok, PaymentMutationError? Error);

public enum PaymentMutationError
{
    NotFound,
    InvalidTransition,
    Conflict,
}
