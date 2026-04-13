using System.Collections.Concurrent;
using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Services.Tickets;

/// <summary>In-memory mock with stable external ids <c>mock-{ticketId:N}</c>; singleton lifetime for cross-request state.</summary>
public sealed class MockTicketProvider : ITicketProvider
{
    private sealed class State
    {
        public string Phase { get; set; } = "Reserved";
    }

    private readonly ConcurrentDictionary<string, State> _states = new();

    public Task<TicketProviderReserveResult> ReserveAsync(
        Guid ticketId,
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ext = $"mock-{ticketId:N}";
        _states[ext] = new State { Phase = "Reserved" };
        var qr = $"QR:RES:{ticketId:N}";
        return Task.FromResult(new TicketProviderReserveResult(ext, qr));
    }

    public Task<TicketProviderPurchaseResult> PurchaseAsync(string externalTicketId, CancellationToken cancellationToken = default)
    {
        if (!_states.TryGetValue(externalTicketId, out var st))
            throw new InvalidOperationException("Unknown external ticket.");

        st.Phase = "Purchased";
        var qr = $"QR:PUR:{externalTicketId}";
        return Task.FromResult(new TicketProviderPurchaseResult(externalTicketId, qr, "Purchased"));
    }

    public Task<TicketProviderSnapshot> GetAsync(string externalTicketId, CancellationToken cancellationToken = default)
    {
        if (!_states.TryGetValue(externalTicketId, out var st))
            throw new InvalidOperationException("Unknown external ticket.");

        var qr = st.Phase == "Purchased" ? $"QR:PUR:{externalTicketId}" : $"QR:RES:{externalTicketId}";
        return Task.FromResult(new TicketProviderSnapshot(externalTicketId, qr, st.Phase));
    }
}
