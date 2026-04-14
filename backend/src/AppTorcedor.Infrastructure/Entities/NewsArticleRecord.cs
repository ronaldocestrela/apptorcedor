using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class NewsArticleRecord
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Summary { get; set; }
    public string Content { get; set; } = "";
    public NewsEditorialStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? UnpublishedAt { get; set; }
}
