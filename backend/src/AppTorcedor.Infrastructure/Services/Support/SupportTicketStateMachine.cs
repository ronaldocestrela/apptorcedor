using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Services.Support;

internal static class SupportTicketStateMachine
{
    public static DateTimeOffset ComputeSlaDeadline(SupportTicketPriority priority, DateTimeOffset now) =>
        priority switch
        {
            SupportTicketPriority.Urgent => now.AddHours(4),
            SupportTicketPriority.High => now.AddHours(24),
            _ => now.AddHours(48),
        };

    public static bool IsValidTransition(SupportTicketStatus from, SupportTicketStatus to)
    {
        if (from == to)
            return false;

        if (from == SupportTicketStatus.Closed)
            return to == SupportTicketStatus.Open;

        return (from, to) switch
        {
            (SupportTicketStatus.Open, SupportTicketStatus.InProgress) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.WaitingUser) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.Open, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.WaitingUser) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.Open) => true,
            (SupportTicketStatus.InProgress, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.WaitingUser, SupportTicketStatus.InProgress) => true,
            (SupportTicketStatus.WaitingUser, SupportTicketStatus.Resolved) => true,
            (SupportTicketStatus.WaitingUser, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.Closed) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.InProgress) => true,
            (SupportTicketStatus.Resolved, SupportTicketStatus.Open) => true,
            _ => false,
        };
    }
}
