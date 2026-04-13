namespace AppTorcedor.Application.Abstractions;

/// <summary>External ticketing gateway (mock in B.8).</summary>
public interface ITicketProvider
{
    Task<TicketProviderReserveResult> ReserveAsync(
        Guid ticketId,
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TicketProviderPurchaseResult> PurchaseAsync(
        string externalTicketId,
        CancellationToken cancellationToken = default);

    Task<TicketProviderSnapshot> GetAsync(string externalTicketId, CancellationToken cancellationToken = default);
}

public sealed record TicketProviderReserveResult(string ExternalTicketId, string QrCodePayload);

public sealed record TicketProviderPurchaseResult(string ExternalTicketId, string QrCodePayload, string ProviderStatus);

public sealed record TicketProviderSnapshot(string ExternalTicketId, string QrCodePayload, string ProviderStatus);
