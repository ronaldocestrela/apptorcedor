namespace AppTorcedor.Application.Abstractions;

public enum NewsEditorialStatus
{
    Draft = 0,
    Published = 1,
    Unpublished = 2,
}

public enum InAppNotificationStatus
{
    Pending = 0,
    Dispatched = 1,
}

public sealed record AdminNewsWriteDto(string Title, string? Summary, string Content);

public sealed record AdminNewsListItemDto(
    Guid NewsId,
    string Title,
    NewsEditorialStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? UnpublishedAt);

public sealed record AdminNewsListPageDto(int TotalCount, IReadOnlyList<AdminNewsListItemDto> Items);

public sealed record AdminNewsDetailDto(
    Guid NewsId,
    string Title,
    string? Summary,
    string Content,
    NewsEditorialStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? UnpublishedAt);

public enum NewsMutationError
{
    NotFound,
    Validation,
    InvalidState,
}

public sealed record NewsMutationResult(bool Ok, NewsMutationError? Error)
{
    public static NewsMutationResult Success() => new(true, null);
    public static NewsMutationResult Fail(NewsMutationError error) => new(false, error);
}

public sealed record NewsCreateResult(Guid? NewsId, NewsMutationError? Error)
{
    public bool Ok => NewsId is not null && Error is null;
}

public enum NewsNotificationError
{
    NotFound,
    Validation,
    InvalidState,
}

public sealed record NewsNotificationCreateResult(bool Ok, int NotificationsCreated, NewsNotificationError? Error)
{
    public static NewsNotificationCreateResult Success(int count) => new(true, count, null);
    public static NewsNotificationCreateResult Fail(NewsNotificationError error) => new(false, 0, error);
}

public interface INewsAdministrationPort
{
    Task<AdminNewsListPageDto> ListNewsAsync(
        string? search,
        NewsEditorialStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminNewsDetailDto?> GetNewsByIdAsync(Guid newsId, CancellationToken cancellationToken = default);

    Task<NewsCreateResult> CreateNewsAsync(AdminNewsWriteDto dto, CancellationToken cancellationToken = default);

    Task<NewsMutationResult> UpdateNewsAsync(Guid newsId, AdminNewsWriteDto dto, CancellationToken cancellationToken = default);

    Task<NewsMutationResult> PublishNewsAsync(Guid newsId, CancellationToken cancellationToken = default);

    Task<NewsMutationResult> UnpublishNewsAsync(Guid newsId, CancellationToken cancellationToken = default);

    Task<NewsNotificationCreateResult> CreateInAppNotificationsForNewsAsync(
        Guid newsId,
        DateTimeOffset? scheduledAt,
        IReadOnlyList<Guid>? targetUserIds,
        CancellationToken cancellationToken = default);
}

public interface IInAppNotificationDispatchService
{
    Task<int> ProcessDueAsync(CancellationToken cancellationToken = default);
}
