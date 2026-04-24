namespace AppTorcedor.Application.Abstractions;

public interface IAdminDashboardReadPort
{
    Task<AdminDashboardDto> GetAsync(CancellationToken cancellationToken = default);
}

public sealed record AdminDashboardDto(
    int ActiveMembersCount,
    int DelinquentMembersCount,
    int OpenSupportTickets,
    decimal TotalFaturadoLast30Days);
