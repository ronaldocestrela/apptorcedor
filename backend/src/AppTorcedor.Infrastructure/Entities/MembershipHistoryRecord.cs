using AppTorcedor.Identity;

namespace AppTorcedor.Infrastructure.Entities;

/// <summary>Append-only operational history for membership (admin actions).</summary>
public sealed class MembershipHistoryRecord
{
    public Guid Id { get; set; }
    public Guid MembershipId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Domain event discriminator, e.g. <see cref="MembershipHistoryEventTypes.StatusChanged"/>.</summary>
    public string EventType { get; set; } = string.Empty;
    public MembershipStatus? FromStatus { get; set; }
    public MembershipStatus ToStatus { get; set; }
    public Guid? FromPlanId { get; set; }
    public Guid? ToPlanId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public static class MembershipHistoryEventTypes
{
    public const string StatusChanged = "StatusChanged";

    /// <summary>Início de contratação pelo torcedor (Parte D.3).</summary>
    public const string Subscribed = "Subscribed";

    /// <summary>Troca de plano pelo torcedor com ajuste proporcional quando aplicável (Parte D.6).</summary>
    public const string PlanChanged = "PlanChanged";
}
