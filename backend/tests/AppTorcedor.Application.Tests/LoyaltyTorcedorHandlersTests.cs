using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetMyLoyaltySummary;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetTorcedorAllTimeLoyaltyRanking;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetTorcedorMonthlyLoyaltyRanking;

namespace AppTorcedor.Application.Tests;

public sealed class LoyaltyTorcedorHandlersTests
{
    [Fact]
    public async Task GetMyLoyaltySummary_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var expected = new LoyaltyTorcedorSummaryDto(10, 3, 1, 2, DateTimeOffset.UtcNow);
        var fake = new FakeLoyaltyTorcedorReadPort { Summary = expected };
        var handler = new GetMyLoyaltySummaryQueryHandler(fake);
        var r = await handler.Handle(new GetMyLoyaltySummaryQuery(uid), CancellationToken.None);
        Assert.Equal(expected, r);
        Assert.Single(fake.SummaryCalls);
        Assert.Equal(uid, fake.SummaryCalls[0]);
    }

    [Fact]
    public async Task GetTorcedorMonthlyLoyaltyRanking_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var page = new LoyaltyTorcedorRankingPageDto(1, [], null);
        var fake = new FakeLoyaltyTorcedorReadPort { MonthlyPage = page };
        var handler = new GetTorcedorMonthlyLoyaltyRankingQueryHandler(fake);
        var r = await handler.Handle(new GetTorcedorMonthlyLoyaltyRankingQuery(uid, 2025, 4, 1, 20), CancellationToken.None);
        Assert.Same(page, r);
        Assert.Single(fake.MonthlyCalls);
        Assert.Equal((uid, 2025, 4, 1, 20), fake.MonthlyCalls[0]);
    }

    [Fact]
    public async Task GetTorcedorAllTimeLoyaltyRanking_delegates_to_port()
    {
        var uid = Guid.NewGuid();
        var page = new LoyaltyTorcedorRankingPageDto(0, [], null);
        var fake = new FakeLoyaltyTorcedorReadPort { AllTimePage = page };
        var handler = new GetTorcedorAllTimeLoyaltyRankingQueryHandler(fake);
        var r = await handler.Handle(new GetTorcedorAllTimeLoyaltyRankingQuery(uid, 2, 10), CancellationToken.None);
        Assert.Same(page, r);
        Assert.Single(fake.AllTimeCalls);
        Assert.Equal((uid, 2, 10), fake.AllTimeCalls[0]);
    }

    private sealed class FakeLoyaltyTorcedorReadPort : ILoyaltyTorcedorReadPort
    {
        public List<Guid> SummaryCalls { get; } = [];
        public List<(Guid UserId, int Year, int Month, int Page, int PageSize)> MonthlyCalls { get; } = [];
        public List<(Guid UserId, int Page, int PageSize)> AllTimeCalls { get; } = [];

        public LoyaltyTorcedorSummaryDto Summary { get; init; } =
            new(0, 0, null, null, DateTimeOffset.UtcNow);

        public LoyaltyTorcedorRankingPageDto MonthlyPage { get; init; } = new(0, [], null);
        public LoyaltyTorcedorRankingPageDto AllTimePage { get; init; } = new(0, [], null);

        public Task<LoyaltyTorcedorSummaryDto> GetMySummaryAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            SummaryCalls.Add(userId);
            return Task.FromResult(Summary);
        }

        public Task<LoyaltyTorcedorRankingPageDto> GetMonthlyRankingAsync(
            Guid currentUserId,
            int year,
            int month,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            MonthlyCalls.Add((currentUserId, year, month, page, pageSize));
            return Task.FromResult(MonthlyPage);
        }

        public Task<LoyaltyTorcedorRankingPageDto> GetAllTimeRankingAsync(
            Guid currentUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            AllTimeCalls.Add((currentUserId, page, pageSize));
            return Task.FromResult(AllTimePage);
        }
    }
}
