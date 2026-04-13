namespace AppTorcedor.Infrastructure.Entities;

public sealed class PrivacyRequestRecord
{
    public Guid Id { get; set; }

    public PrivacyRequestKind Kind { get; set; }

    public Guid SubjectUserId { get; set; }

    public Guid RequestedByUserId { get; set; }

    public PrivacyRequestStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ResultJson { get; set; }

    public string? ErrorMessage { get; set; }
}
