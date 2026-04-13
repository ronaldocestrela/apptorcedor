using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetNewsFeed;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetPublishedNewsDetail;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListEligibleBenefitOffers;
using AppTorcedor.Application.Modules.Torcedor.Queries.ListPublishedPlans;

namespace AppTorcedor.Application.Tests;

public sealed class TorcedorConsumptionHandlersTests
{
    [Fact]
    public async Task GetNewsFeed_delegates_to_port()
    {
        var fake = new FakeTorcedorNewsPort();
        var handler = new GetNewsFeedQueryHandler(fake);
        var page = await handler.Handle(new GetNewsFeedQuery("q", 2, 15), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal("q", fake.ListCalls[0].Search);
        Assert.Equal(2, fake.ListCalls[0].Page);
        Assert.Equal(15, fake.ListCalls[0].PageSize);
    }

    [Fact]
    public async Task GetPublishedNewsDetail_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeTorcedorNewsPort();
        var handler = new GetPublishedNewsDetailQueryHandler(fake);
        await handler.Handle(new GetPublishedNewsDetailQuery(id), CancellationToken.None);
        Assert.Single(fake.GetByIdCalls);
        Assert.Equal(id, fake.GetByIdCalls[0]);
    }

    [Fact]
    public async Task ListEligibleBenefitOffers_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var fake = new FakeTorcedorBenefitsPort();
        var handler = new ListEligibleBenefitOffersQueryHandler(fake);
        await handler.Handle(new ListEligibleBenefitOffersQuery(uid, 1, 10), CancellationToken.None);
        Assert.Single(fake.ListEligibleCalls);
        Assert.Equal(uid, fake.ListEligibleCalls[0].UserId);
    }

    [Fact]
    public async Task ListPublishedPlans_delegates_to_port()
    {
        var fake = new FakeTorcedorPublishedPlansPort();
        var handler = new ListPublishedPlansQueryHandler(fake);
        var catalog = await handler.Handle(new ListPublishedPlansQuery(), CancellationToken.None);
        Assert.NotNull(catalog);
        Assert.Single(fake.ListCalls);
    }

    private sealed class FakeTorcedorNewsPort : ITorcedorNewsReadPort
    {
        public List<(string? Search, int Page, int PageSize)> ListCalls { get; } = [];
        public List<Guid> GetByIdCalls { get; } = [];

        public Task<TorcedorNewsFeedPageDto> ListPublishedAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((search, page, pageSize));
            return Task.FromResult(new TorcedorNewsFeedPageDto(0, []));
        }

        public Task<TorcedorNewsDetailDto?> GetPublishedByIdAsync(Guid newsId, CancellationToken cancellationToken = default)
        {
            GetByIdCalls.Add(newsId);
            return Task.FromResult<TorcedorNewsDetailDto?>(null);
        }
    }

    private sealed class FakeTorcedorBenefitsPort : ITorcedorBenefitsReadPort
    {
        public List<(Guid UserId, int Page, int PageSize)> ListEligibleCalls { get; } = [];

        public Task<TorcedorEligibleBenefitOffersPageDto> ListEligibleForUserAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListEligibleCalls.Add((userId, page, pageSize));
            return Task.FromResult(new TorcedorEligibleBenefitOffersPageDto(0, []));
        }
    }

    private sealed class FakeTorcedorPublishedPlansPort : ITorcedorPublishedPlansReadPort
    {
        public List<int> ListCalls { get; } = [];

        public Task<TorcedorPublishedPlansCatalogDto> ListPublishedActiveAsync(CancellationToken cancellationToken = default)
        {
            ListCalls.Add(1);
            return Task.FromResult(new TorcedorPublishedPlansCatalogDto([]));
        }
    }
}
