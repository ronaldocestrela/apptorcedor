namespace AppTorcedor.Api.Contracts;

public sealed record AdminDashboardResponse(
    int ActiveMembersCount,
    int DelinquentMembersCount,
    int OpenSupportTickets,
    decimal TotalFaturadoLast30Days);
