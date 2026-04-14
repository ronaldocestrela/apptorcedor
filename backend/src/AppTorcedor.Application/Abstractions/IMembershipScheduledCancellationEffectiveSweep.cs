namespace AppTorcedor.Application.Abstractions;

/// <summary>Efetiva cancelamentos agendados (D.7): memberships ativas com <c>EndDate</c> vencido passam a canceladas.</summary>
public interface IMembershipScheduledCancellationEffectiveSweep
{
    Task<int> ApplyAsync(CancellationToken cancellationToken = default);

    /// <summary>Para testes com relógio fixo.</summary>
    Task<int> ApplyAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
}
