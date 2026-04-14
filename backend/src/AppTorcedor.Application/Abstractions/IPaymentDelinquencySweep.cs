namespace AppTorcedor.Application.Abstractions;

public interface IPaymentDelinquencySweep
{
    Task<PaymentDelinquencySweepResult> RunAsync(CancellationToken cancellationToken = default);
}

public sealed record PaymentDelinquencySweepResult(
    int PaymentsMarkedOverdue,
    int MembershipsMarkedDelinquent,
    int MembershipsEffectivelyCancelled = 0);
