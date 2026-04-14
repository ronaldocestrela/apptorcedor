using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreateAdminNews;
using AppTorcedor.Application.Modules.Administration.Commands.CreateNewsInAppNotifications;
using AppTorcedor.Application.Modules.Administration.Commands.PublishAdminNews;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminNews;

namespace AppTorcedor.Application.Tests;

public sealed class NewsAdminHandlersTests
{
    [Fact]
    public async Task ListAdminNews_delegates_to_port()
    {
        var fake = new FakeNewsPort();
        var handler = new ListAdminNewsQueryHandler(fake);
        var page = await handler.Handle(new ListAdminNewsQuery("x", NewsEditorialStatus.Draft, 1, 10), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal("x", fake.ListCalls[0].Search);
        Assert.Equal(NewsEditorialStatus.Draft, fake.ListCalls[0].Status);
    }

    [Fact]
    public async Task CreateAdminNews_delegates_to_port()
    {
        var dto = new AdminNewsWriteDto("T", "S", "Body");
        var fake = new FakeNewsPort { CreateResult = new NewsCreateResult(Guid.NewGuid(), null) };
        var handler = new CreateAdminNewsCommandHandler(fake);
        var r = await handler.Handle(new CreateAdminNewsCommand(dto), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.CreateCalls);
    }

    [Fact]
    public async Task PublishAdminNews_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeNewsPort { Mutation = NewsMutationResult.Success() };
        var handler = new PublishAdminNewsCommandHandler(fake);
        var r = await handler.Handle(new PublishAdminNewsCommand(id), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.PublishCalls);
        Assert.Equal(id, fake.PublishCalls[0]);
    }

    [Fact]
    public async Task CreateNewsInAppNotifications_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeNewsPort { NotificationResult = new NewsNotificationCreateResult(true, 3, null) };
        var handler = new CreateNewsInAppNotificationsCommandHandler(fake);
        var r = await handler.Handle(
            new CreateNewsInAppNotificationsCommand(id, null, null),
            CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Equal(3, r.NotificationsCreated);
        Assert.Single(fake.NotifyCalls);
    }

    private sealed class FakeNewsPort : INewsAdministrationPort
    {
        public List<(string? Search, NewsEditorialStatus? Status, int Page, int PageSize)> ListCalls { get; } = [];
        public List<Guid> GetCalls { get; } = [];
        public List<AdminNewsWriteDto> CreateCalls { get; } = [];
        public List<(Guid Id, AdminNewsWriteDto Dto)> UpdateCalls { get; } = [];
        public List<Guid> PublishCalls { get; } = [];
        public List<Guid> UnpublishCalls { get; } = [];
        public List<(Guid NewsId, DateTimeOffset? ScheduledAt, IReadOnlyList<Guid>? UserIds)> NotifyCalls { get; } = [];

        public NewsCreateResult CreateResult { get; init; } = new(null, NewsMutationError.Validation);
        public NewsMutationResult Mutation { get; init; } = NewsMutationResult.Fail(NewsMutationError.NotFound);
        public NewsNotificationCreateResult NotificationResult { get; init; } = new(false, 0, NewsNotificationError.NotFound);

        public Task<AdminNewsListPageDto> ListNewsAsync(
            string? search,
            NewsEditorialStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((search, status, page, pageSize));
            return Task.FromResult(new AdminNewsListPageDto(0, []));
        }

        public Task<AdminNewsDetailDto?> GetNewsByIdAsync(Guid newsId, CancellationToken cancellationToken = default)
        {
            GetCalls.Add(newsId);
            return Task.FromResult<AdminNewsDetailDto?>(null);
        }

        public Task<NewsCreateResult> CreateNewsAsync(AdminNewsWriteDto dto, CancellationToken cancellationToken = default)
        {
            CreateCalls.Add(dto);
            return Task.FromResult(CreateResult);
        }

        public Task<NewsMutationResult> UpdateNewsAsync(Guid newsId, AdminNewsWriteDto dto, CancellationToken cancellationToken = default)
        {
            UpdateCalls.Add((newsId, dto));
            return Task.FromResult(Mutation);
        }

        public Task<NewsMutationResult> PublishNewsAsync(Guid newsId, CancellationToken cancellationToken = default)
        {
            PublishCalls.Add(newsId);
            return Task.FromResult(Mutation);
        }

        public Task<NewsMutationResult> UnpublishNewsAsync(Guid newsId, CancellationToken cancellationToken = default)
        {
            UnpublishCalls.Add(newsId);
            return Task.FromResult(Mutation);
        }

        public Task<NewsNotificationCreateResult> CreateInAppNotificationsForNewsAsync(
            Guid newsId,
            DateTimeOffset? scheduledAt,
            IReadOnlyList<Guid>? targetUserIds,
            CancellationToken cancellationToken = default)
        {
            NotifyCalls.Add((newsId, scheduledAt, targetUserIds));
            return Task.FromResult(NotificationResult);
        }
    }
}
