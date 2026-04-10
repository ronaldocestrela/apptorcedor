using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Domain.Rules;

public sealed class MemberStatusTransitionRule(MemberStatus current, MemberStatus next) : IBusinessRule
{
    public string Message =>
        $"Transition from {current} to {next} is not allowed.";

    public bool IsBroken()
    {
        if (current == next)
            return false;

        return !IsAllowed(current, next);
    }

    private static bool IsAllowed(MemberStatus from, MemberStatus to) =>
        (from, to) switch
        {
            (MemberStatus.PendingCompletion, MemberStatus.Active) => true,
            (MemberStatus.PendingCompletion, MemberStatus.Canceled) => true,
            (MemberStatus.Active, MemberStatus.Delinquent) => true,
            (MemberStatus.Active, MemberStatus.Canceled) => true,
            (MemberStatus.Active, MemberStatus.Suspended) => true,
            (MemberStatus.Delinquent, MemberStatus.Active) => true,
            (MemberStatus.Delinquent, MemberStatus.Canceled) => true,
            (MemberStatus.Delinquent, MemberStatus.Suspended) => true,
            (MemberStatus.Suspended, MemberStatus.Active) => true,
            (MemberStatus.Suspended, MemberStatus.Delinquent) => true,
            (MemberStatus.Suspended, MemberStatus.Canceled) => true,
            (MemberStatus.Canceled, MemberStatus.Active) => true,
            _ => false
        };
}
