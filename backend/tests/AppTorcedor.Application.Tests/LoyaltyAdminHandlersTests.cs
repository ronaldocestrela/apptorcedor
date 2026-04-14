using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreateLoyaltyCampaign;
using AppTorcedor.Application.Modules.Administration.Commands.ManualLoyaltyPointsAdjustment;
using AppTorcedor.Application.Modules.Administration.Commands.PublishLoyaltyCampaign;
using AppTorcedor.Application.Modules.Administration.Queries.ListLoyaltyCampaigns;

namespace AppTorcedor.Application.Tests;

public sealed class LoyaltyAdminHandlersTests
{
    [Fact]
    public async Task ListLoyaltyCampaigns_delegates_to_port()
    {
        var fake = new FakeLoyaltyPort();
        var handler = new ListLoyaltyCampaignsQueryHandler(fake);
        var page = await handler.Handle(new ListLoyaltyCampaignsQuery(LoyaltyCampaignStatus.Draft, 1, 10), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
    }

    [Fact]
    public async Task CreateLoyaltyCampaign_delegates_to_port()
    {
        var dto = new LoyaltyCampaignWriteDto("C", null, [new LoyaltyPointRuleWriteDto(LoyaltyPointRuleTrigger.PaymentPaid, 10, 0)]);
        var fake = new FakeLoyaltyPort { CreateResult = new LoyaltyCampaignCreateResult(Guid.NewGuid(), null) };
        var handler = new CreateLoyaltyCampaignCommandHandler(fake);
        var r = await handler.Handle(new CreateLoyaltyCampaignCommand(dto), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.CreateCalls);
    }

    [Fact]
    public async Task PublishLoyaltyCampaign_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeLoyaltyPort { Mutation = LoyaltyMutationResult.Success() };
        var handler = new PublishLoyaltyCampaignCommandHandler(fake);
        var r = await handler.Handle(new PublishLoyaltyCampaignCommand(id), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.PublishCalls);
    }

    [Fact]
    public async Task ManualLoyaltyPoints_delegates_to_port()
    {
        var fake = new FakeLoyaltyPort { Manual = LoyaltyManualAdjustResult.Success() };
        var handler = new ManualLoyaltyPointsAdjustmentCommandHandler(fake);
        var r = await handler.Handle(
            new ManualLoyaltyPointsAdjustmentCommand(Guid.NewGuid(), 5, "bonus", null, Guid.NewGuid()),
            CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.ManualCalls);
    }

    private sealed class FakeLoyaltyPort : ILoyaltyAdministrationPort
    {
        public List<(LoyaltyCampaignStatus? Status, int Page, int PageSize)> ListCalls { get; } = [];
        public List<LoyaltyCampaignWriteDto> CreateCalls { get; } = [];
        public List<Guid> PublishCalls { get; } = [];

        public List<(Guid UserId, int Points, string Reason, Guid? CampaignId, Guid Actor)> ManualCalls { get; } =
            [];

        public LoyaltyCampaignCreateResult CreateResult { get; init; } = new(null, LoyaltyMutationError.Validation);
        public LoyaltyMutationResult Mutation { get; init; } = LoyaltyMutationResult.Fail(LoyaltyMutationError.NotFound);
        public LoyaltyManualAdjustResult Manual { get; init; } = LoyaltyManualAdjustResult.Fail(LoyaltyManualAdjustError.Validation);

        public Task<LoyaltyCampaignListPageDto> ListCampaignsAsync(
            LoyaltyCampaignStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((status, page, pageSize));
            return Task.FromResult(new LoyaltyCampaignListPageDto(0, []));
        }

        public Task<LoyaltyCampaignDetailDto?> GetCampaignByIdAsync(Guid campaignId, CancellationToken cancellationToken = default) =>
            Task.FromResult<LoyaltyCampaignDetailDto?>(null);

        public Task<LoyaltyCampaignCreateResult> CreateCampaignAsync(LoyaltyCampaignWriteDto dto, CancellationToken cancellationToken = default)
        {
            CreateCalls.Add(dto);
            return Task.FromResult(CreateResult);
        }

        public Task<LoyaltyMutationResult> UpdateCampaignAsync(
            Guid campaignId,
            LoyaltyCampaignWriteDto dto,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Mutation);

        public Task<LoyaltyMutationResult> PublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default)
        {
            PublishCalls.Add(campaignId);
            return Task.FromResult(Mutation);
        }

        public Task<LoyaltyMutationResult> UnpublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Mutation);

        public Task<LoyaltyManualAdjustResult> ManualAdjustAsync(
            Guid userId,
            int points,
            string reason,
            Guid? campaignId,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            ManualCalls.Add((userId, points, reason, campaignId, actorUserId));
            return Task.FromResult(Manual);
        }

        public Task<LoyaltyLedgerPageDto> ListUserLedgerAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new LoyaltyLedgerPageDto(0, []));

        public Task<LoyaltyRankingPageDto> GetMonthlyRankingAsync(
            int year,
            int month,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new LoyaltyRankingPageDto(0, []));

        public Task<LoyaltyRankingPageDto> GetAllTimeRankingAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LoyaltyRankingPageDto(0, []));
    }
}
