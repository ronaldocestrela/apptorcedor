using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreateBenefitPartner;
using AppTorcedor.Application.Modules.Administration.Commands.RedeemBenefitOffer;
using AppTorcedor.Application.Modules.Administration.Commands.UploadBenefitOfferBanner;
using AppTorcedor.Application.Modules.Administration.Queries.ListBenefitPartners;
using AppTorcedor.Identity;

namespace AppTorcedor.Application.Tests;

public sealed class BenefitsAdminHandlersTests
{
    [Fact]
    public async Task ListBenefitPartners_delegates_to_port()
    {
        var fake = new FakeBenefitsPort();
        var handler = new ListBenefitPartnersQueryHandler(fake);
        var page = await handler.Handle(new ListBenefitPartnersQuery("x", true, 1, 10), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListPartnerCalls);
    }

    [Fact]
    public async Task CreateBenefitPartner_delegates_to_port()
    {
        var dto = new BenefitPartnerWriteDto("P", "d", true);
        var fake = new FakeBenefitsPort { CreatePartnerResult = new BenefitCreateResult(Guid.NewGuid(), null) };
        var handler = new CreateBenefitPartnerCommandHandler(fake);
        var r = await handler.Handle(new CreateBenefitPartnerCommand(dto), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.CreatePartnerCalls);
    }

    [Fact]
    public async Task RedeemBenefitOffer_delegates_to_port()
    {
        var fake = new FakeBenefitsPort { Redeem = BenefitRedeemResult.Success(Guid.NewGuid()) };
        var handler = new RedeemBenefitOfferCommandHandler(fake);
        var r = await handler.Handle(
            new RedeemBenefitOfferCommand(Guid.NewGuid(), Guid.NewGuid(), "n", Guid.NewGuid()),
            CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.RedeemCalls);
    }

    [Fact]
    public async Task UploadBenefitOfferBanner_delegates_to_port()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        var fake = new FakeBenefitsPort();
        var handler = new UploadBenefitOfferBannerCommandHandler(fake);
        var oid = Guid.NewGuid();
        _ = await handler.Handle(new UploadBenefitOfferBannerCommand(oid, stream, "a.png", "image/png"), CancellationToken.None);
        Assert.Single(fake.UploadBannerCalls);
        Assert.Equal(oid, fake.UploadBannerCalls[0].OfferId);
    }

    private sealed class FakeBenefitsPort : IBenefitsAdministrationPort
    {
        public List<(string? Search, bool? IsActive, int Page, int PageSize)> ListPartnerCalls { get; } = [];
        public List<BenefitPartnerWriteDto> CreatePartnerCalls { get; } = [];
        public List<(Guid OfferId, Guid UserId, string? Notes, Guid Actor)> RedeemCalls { get; } = [];

        public BenefitCreateResult CreatePartnerResult { get; init; } = new(null, BenefitMutationError.Validation);
        public BenefitRedeemResult Redeem { get; init; } = BenefitRedeemResult.Fail(BenefitMutationError.NotFound);

        public Task<BenefitPartnerListPageDto> ListPartnersAsync(
            string? search,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListPartnerCalls.Add((search, isActive, page, pageSize));
            return Task.FromResult(new BenefitPartnerListPageDto(0, []));
        }

        public Task<BenefitPartnerDetailDto?> GetPartnerByIdAsync(Guid partnerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BenefitPartnerDetailDto?>(null);

        public Task<BenefitCreateResult> CreatePartnerAsync(BenefitPartnerWriteDto dto, CancellationToken cancellationToken = default)
        {
            CreatePartnerCalls.Add(dto);
            return Task.FromResult(CreatePartnerResult);
        }

        public Task<BenefitMutationResult> UpdatePartnerAsync(
            Guid partnerId,
            BenefitPartnerWriteDto dto,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(BenefitMutationResult.Fail(BenefitMutationError.NotFound));

        public Task<BenefitOfferListPageDto> ListOffersAsync(
            Guid? partnerId,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BenefitOfferListPageDto(0, []));

        public Task<BenefitOfferDetailDto?> GetOfferByIdAsync(Guid offerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BenefitOfferDetailDto?>(null);

        public Task<BenefitCreateResult> CreateOfferAsync(BenefitOfferWriteDto dto, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BenefitCreateResult(null, BenefitMutationError.Validation));

        public Task<BenefitMutationResult> UpdateOfferAsync(
            Guid offerId,
            BenefitOfferWriteDto dto,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(BenefitMutationResult.Fail(BenefitMutationError.NotFound));

        public List<(Guid OfferId, string FileName)> UploadBannerCalls { get; } = [];

        public Task<BenefitBannerUploadResult> UploadOfferBannerAsync(
            Guid offerId,
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            UploadBannerCalls.Add((offerId, fileName));
            return Task.FromResult(BenefitBannerUploadResult.Fail(BenefitMutationError.Validation));
        }

        public Task<BenefitMutationResult> RemoveOfferBannerAsync(Guid offerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(BenefitMutationResult.Fail(BenefitMutationError.NotFound));

        public Task<BenefitRedeemResult> RedeemOfferAsync(
            Guid offerId,
            Guid userId,
            string? notes,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            RedeemCalls.Add((offerId, userId, notes, actorUserId));
            return Task.FromResult(Redeem);
        }

        public Task<BenefitRedemptionListPageDto> ListRedemptionsAsync(
            Guid? offerId,
            Guid? userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BenefitRedemptionListPageDto(0, []));
    }
}
