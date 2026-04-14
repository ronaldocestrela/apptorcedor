using System.ComponentModel.DataAnnotations;

namespace AppTorcedor.Api.Contracts;

public sealed class UpsertNewsRequest
{
    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = "";

    [MaxLength(2000)]
    public string? Summary { get; set; }

    [Required]
    [MaxLength(200000)]
    public string Content { get; set; } = "";
}

public sealed class CreateNewsNotificationsRequest
{
    public DateTimeOffset? ScheduledAt { get; set; }

    public IReadOnlyList<Guid>? UserIds { get; set; }
}
