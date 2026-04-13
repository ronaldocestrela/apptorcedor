using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreatePlan;
using AppTorcedor.Application.Modules.Administration.Commands.UpdatePlan;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminPlanDetail;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminPlans;

namespace AppTorcedor.Application.Tests;

public sealed class PlansAdminHandlersTests
{
    [Fact]
    public async Task ListAdminPlans_delegates_to_port()
    {
        var fake = new FakePlansPort();
        var handler = new ListAdminPlansQueryHandler(fake);
        var page = await handler.Handle(new ListAdminPlansQuery("gold", true, false, 2, 10), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal("gold", fake.ListCalls[0].Search);
        Assert.True(fake.ListCalls[0].IsActive);
        Assert.False(fake.ListCalls[0].IsPublished);
        Assert.Equal(2, fake.ListCalls[0].Page);
        Assert.Equal(10, fake.ListCalls[0].PageSize);
    }

    [Fact]
    public async Task GetAdminPlanDetail_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakePlansPort();
        var handler = new GetAdminPlanDetailQueryHandler(fake);
        var detail = await handler.Handle(new GetAdminPlanDetailQuery(id), CancellationToken.None);
        Assert.Null(detail);
        Assert.Single(fake.DetailCalls);
        Assert.Equal(id, fake.DetailCalls[0]);
    }

    [Fact]
    public async Task CreatePlan_returns_validation_error_when_publish_inactive()
    {
        var fake = new FakePlansPort();
        var handler = new CreatePlanCommandHandler(fake);
        var dto = new AdminPlanWriteDto(
            "X",
            10,
            "Monthly",
            0,
            false,
            true,
            null,
            null,
            []);
        var result = await handler.Handle(new CreatePlanCommand(dto), CancellationToken.None);
        Assert.False(result.Ok);
        Assert.Null(result.PlanId);
        Assert.Contains("publish", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Empty(fake.CreateCalls);
    }

    [Fact]
    public async Task CreatePlan_delegates_when_valid()
    {
        var fake = new FakePlansPort { NewPlanId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee") };
        var handler = new CreatePlanCommandHandler(fake);
        var dto = new AdminPlanWriteDto(
            "Gold",
            99.9m,
            "monthly",
            5,
            true,
            false,
            "Resumo",
            null,
            [new AdminPlanBenefitInputDto(0, "Ingresso", "Desconto")]);
        var result = await handler.Handle(new CreatePlanCommand(dto), CancellationToken.None);
        Assert.True(result.Ok);
        Assert.Equal(fake.NewPlanId, result.PlanId);
        Assert.Single(fake.CreateCalls);
        Assert.Equal("Monthly", fake.CreateCalls[0].BillingCycle);
    }

    [Fact]
    public async Task UpdatePlan_returns_NotFound_when_port_returns_false()
    {
        var id = Guid.NewGuid();
        var fake = new FakePlansPort();
        var handler = new UpdatePlanCommandHandler(fake);
        var dto = SampleValidDto();
        var result = await handler.Handle(new UpdatePlanCommand(id, dto), CancellationToken.None);
        Assert.False(result.Ok);
        Assert.True(result.NotFound);
        Assert.Single(fake.UpdateCalls);
    }

    [Fact]
    public async Task UpdatePlan_succeeds_when_port_updates()
    {
        var id = Guid.NewGuid();
        var fake = new FakePlansPort { UpdateReturnsTrue = true };
        var handler = new UpdatePlanCommandHandler(fake);
        var dto = SampleValidDto();
        var result = await handler.Handle(new UpdatePlanCommand(id, dto), CancellationToken.None);
        Assert.True(result.Ok);
        Assert.False(result.NotFound);
        Assert.Null(result.ValidationError);
        Assert.Single(fake.UpdateCalls);
        Assert.Equal(id, fake.UpdateCalls[0].PlanId);
    }

    private static AdminPlanWriteDto SampleValidDto() =>
        new(
            "Silver",
            50,
            "Yearly",
            0,
            true,
            true,
            null,
            "Regra básica",
            [new AdminPlanBenefitInputDto(1, "Loja", null)]);

    private sealed class FakePlansPort : IPlansAdministrationPort
    {
        public Guid NewPlanId { get; init; } = Guid.NewGuid();
        public bool UpdateReturnsTrue { get; init; }

        public List<(string? Search, bool? IsActive, bool? IsPublished, int Page, int PageSize)> ListCalls { get; } = [];
        public List<Guid> DetailCalls { get; } = [];
        public List<AdminPlanWriteDto> CreateCalls { get; } = [];
        public List<(Guid PlanId, AdminPlanWriteDto Dto)> UpdateCalls { get; } = [];

        public Task<AdminPlanListPageDto> ListPlansAsync(
            string? search,
            bool? isActive,
            bool? isPublished,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((search, isActive, isPublished, page, pageSize));
            return Task.FromResult(new AdminPlanListPageDto(0, []));
        }

        public Task<AdminPlanDetailDto?> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default)
        {
            DetailCalls.Add(planId);
            return Task.FromResult<AdminPlanDetailDto?>(null);
        }

        public Task<Guid> CreatePlanAsync(AdminPlanWriteDto dto, CancellationToken cancellationToken = default)
        {
            CreateCalls.Add(dto);
            return Task.FromResult(NewPlanId);
        }

        public Task<bool> UpdatePlanAsync(Guid planId, AdminPlanWriteDto dto, CancellationToken cancellationToken = default)
        {
            UpdateCalls.Add((planId, dto));
            return Task.FromResult(UpdateReturnsTrue);
        }
    }
}
