namespace SocioTorcedor.Modules.Membership.Domain.Enums;

/// <summary>
/// Stored as int in the database. Legacy values: 2 = Inactive (migrated to Canceled), 3 = Suspended (migrated to new code4).
/// </summary>
public enum MemberStatus
{
    PendingCompletion = 0,
    Active = 1,
    Delinquent = 2,
    Canceled = 3,
    Suspended = 4
}
