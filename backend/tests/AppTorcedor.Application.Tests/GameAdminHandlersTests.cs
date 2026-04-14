using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreateAdminGame;
using AppTorcedor.Application.Modules.Administration.Commands.DeactivateAdminGame;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminGame;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminGame;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminGames;

namespace AppTorcedor.Application.Tests;

public sealed class GameAdminHandlersTests
{
    [Fact]
    public async Task ListAdminGames_delegates_to_port()
    {
        var fake = new FakeGamePort();
        var handler = new ListAdminGamesQueryHandler(fake);
        var page = await handler.Handle(new ListAdminGamesQuery("abc", true, 2, 15), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal("abc", fake.ListCalls[0].Search);
        Assert.True(fake.ListCalls[0].IsActive);
    }

    [Fact]
    public async Task GetAdminGame_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeGamePort();
        var handler = new GetAdminGameQueryHandler(fake);
        var detail = await handler.Handle(new GetAdminGameQuery(id), CancellationToken.None);
        Assert.Null(detail);
        Assert.Single(fake.GetCalls);
        Assert.Equal(id, fake.GetCalls[0]);
    }

    [Fact]
    public async Task CreateAdminGame_delegates_to_port()
    {
        var dto = new AdminGameWriteDto("A", "B", DateTimeOffset.UtcNow, true);
        var fake = new FakeGamePort { CreateResult = new GameCreateResult(Guid.NewGuid(), null) };
        var handler = new CreateAdminGameCommandHandler(fake);
        var r = await handler.Handle(new CreateAdminGameCommand(dto), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.CreateCalls);
    }

    [Fact]
    public async Task UpdateAdminGame_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var dto = new AdminGameWriteDto("A", "B", DateTimeOffset.UtcNow, true);
        var fake = new FakeGamePort { Mutation = GameMutationResult.Success() };
        var handler = new UpdateAdminGameCommandHandler(fake);
        var r = await handler.Handle(new UpdateAdminGameCommand(id, dto), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.UpdateCalls);
        Assert.Equal(id, fake.UpdateCalls[0].GameId);
    }

    [Fact]
    public async Task DeactivateAdminGame_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeGamePort { Mutation = GameMutationResult.Success() };
        var handler = new DeactivateAdminGameCommandHandler(fake);
        var r = await handler.Handle(new DeactivateAdminGameCommand(id), CancellationToken.None);
        Assert.True(r.Ok);
        Assert.Single(fake.DeactivateCalls);
        Assert.Equal(id, fake.DeactivateCalls[0]);
    }

    private sealed class FakeGamePort : IGameAdministrationPort
    {
        public List<(string? Search, bool? IsActive, int Page, int PageSize)> ListCalls { get; } = [];
        public List<Guid> GetCalls { get; } = [];
        public List<AdminGameWriteDto> CreateCalls { get; } = [];
        public List<(Guid GameId, AdminGameWriteDto Dto)> UpdateCalls { get; } = [];
        public List<Guid> DeactivateCalls { get; } = [];

        public GameCreateResult CreateResult { get; init; } = new(null, GameMutationError.Validation);
        public GameMutationResult Mutation { get; init; } = GameMutationResult.Fail(GameMutationError.NotFound);

        public Task<AdminGameListPageDto> ListGamesAsync(
            string? search,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((search, isActive, page, pageSize));
            return Task.FromResult(new AdminGameListPageDto(0, []));
        }

        public Task<AdminGameDetailDto?> GetGameByIdAsync(Guid gameId, CancellationToken cancellationToken = default)
        {
            GetCalls.Add(gameId);
            return Task.FromResult<AdminGameDetailDto?>(null);
        }

        public Task<GameCreateResult> CreateGameAsync(AdminGameWriteDto dto, CancellationToken cancellationToken = default)
        {
            CreateCalls.Add(dto);
            return Task.FromResult(CreateResult);
        }

        public Task<GameMutationResult> UpdateGameAsync(Guid gameId, AdminGameWriteDto dto, CancellationToken cancellationToken = default)
        {
            UpdateCalls.Add((gameId, dto));
            return Task.FromResult(Mutation);
        }

        public Task<GameMutationResult> DeactivateGameAsync(Guid gameId, CancellationToken cancellationToken = default)
        {
            DeactivateCalls.Add(gameId);
            return Task.FromResult(Mutation);
        }
    }
}
