using System.ComponentModel.DataAnnotations;
using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Api.Contracts;

public sealed class CreateSupportTicketRequest
{
    [Required]
    public Guid RequesterUserId { get; set; }

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string Queue { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;

    [StringLength(8000)]
    public string? InitialMessage { get; set; }
}

public sealed class ReplySupportTicketRequest
{
    [Required]
    [StringLength(8000, MinimumLength = 1)]
    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }
}

public sealed class AssignSupportTicketRequest
{
    public Guid? AgentUserId { get; set; }
}

public sealed class ChangeSupportTicketStatusRequest
{
    [Required]
    public SupportTicketStatus Status { get; set; }

    [StringLength(2000)]
    public string? Reason { get; set; }
}
