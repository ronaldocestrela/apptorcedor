namespace AppTorcedor.Infrastructure.Entities;

/// <summary>Pending staff onboarding via invite token (single-tenant backoffice).</summary>
public sealed class StaffInviteRecord
{
    public Guid Id { get; set; }

    /// <summary>Identity-normalized email (uppercase invariant).</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>Email as entered by admin (used for account creation / display).</summary>
    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    /// <summary>JSON array of role names to assign on acceptance.</summary>
    public string RolesJson { get; set; } = "[]";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }
}
