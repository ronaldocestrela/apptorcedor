using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using AppTorcedor.Application.Modules.Users.Commands.SetUserAccountActive;
using AppTorcedor.Application.Modules.Users.Commands.UpsertAdminUserProfile;
using AppTorcedor.Application.Modules.Users.Queries.GetAdminUserDetail;
using AppTorcedor.Application.Modules.Users.Queries.ListAdminUsers;

namespace AppTorcedor.Application.Tests;

public sealed class AdminUsersHandlersTests
{
    [Fact]
    public async Task ListAdminUsers_delegates_to_port()
    {
        var fake = new FakeUserAdministrationPort();
        var handler = new ListAdminUsersQueryHandler(fake);
        var page = await handler.Handle(new ListAdminUsersQuery("x", true, 2, 10), CancellationToken.None);
        Assert.Equal(0, page.TotalCount);
        Assert.Single(fake.ListCalls);
        Assert.Equal("x", fake.ListCalls[0].Search);
        Assert.True(fake.ListCalls[0].IsActive);
        Assert.Equal(2, fake.ListCalls[0].Page);
        Assert.Equal(10, fake.ListCalls[0].PageSize);
    }

    [Fact]
    public async Task GetAdminUserDetail_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeUserAdministrationPort();
        var handler = new GetAdminUserDetailQueryHandler(fake);
        var detail = await handler.Handle(new GetAdminUserDetailQuery(id), CancellationToken.None);
        Assert.Null(detail);
        Assert.Single(fake.DetailCalls);
        Assert.Equal(id, fake.DetailCalls[0]);
    }

    [Fact]
    public async Task SetUserAccountActive_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var fake = new FakeUserAdministrationPort { SetActiveResult = true };
        var handler = new SetUserAccountActiveCommandHandler(fake);
        var ok = await handler.Handle(new SetUserAccountActiveCommand(id, false), CancellationToken.None);
        Assert.True(ok);
        Assert.Single(fake.SetActiveCalls);
        Assert.Equal(id, fake.SetActiveCalls[0].UserId);
        Assert.False(fake.SetActiveCalls[0].IsActive);
    }

    [Fact]
    public async Task UpsertAdminUserProfile_delegates_to_port()
    {
        var id = Guid.NewGuid();
        var patch = new AdminUserProfileUpsertDto("123", new DateOnly(1990, 1, 2), null, "Rua A", "note");
        var fake = new FakeUserAdministrationPort { UpsertResult = ProfileUpsertResult.Ok() };
        var handler = new UpsertAdminUserProfileCommandHandler(fake);
        var ok = await handler.Handle(new UpsertAdminUserProfileCommand(id, patch), CancellationToken.None);
        Assert.True(ok.Succeeded);
        Assert.Single(fake.UpsertCalls);
        Assert.Equal(id, fake.UpsertCalls[0].UserId);
        Assert.Equal("123", fake.UpsertCalls[0].Patch.Document);
    }

    private sealed class FakeUserAdministrationPort : IUserAdministrationPort
    {
        public List<(string? Search, bool? IsActive, int Page, int PageSize)> ListCalls { get; } = [];
        public List<Guid> DetailCalls { get; } = [];
        public List<(Guid UserId, bool IsActive)> SetActiveCalls { get; } = [];
        public List<(Guid UserId, AdminUserProfileUpsertDto Patch)> UpsertCalls { get; } = [];

        public bool SetActiveResult { get; init; }
        public ProfileUpsertResult UpsertResult { get; init; } = ProfileUpsertResult.Ok();

        public Task<AdminUserListPageDto> ListUsersAsync(
            string? search,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ListCalls.Add((search, isActive, page, pageSize));
            return Task.FromResult(new AdminUserListPageDto(0, []));
        }

        public Task<AdminUserDetailDto?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            DetailCalls.Add(userId);
            return Task.FromResult<AdminUserDetailDto?>(null);
        }

        public Task<bool> SetAccountActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
        {
            SetActiveCalls.Add((userId, isActive));
            return Task.FromResult(SetActiveResult);
        }

        public Task<ProfileUpsertResult> UpsertProfileAsync(
            Guid userId,
            AdminUserProfileUpsertDto patch,
            CancellationToken cancellationToken = default)
        {
            UpsertCalls.Add((userId, patch));
            return Task.FromResult(UpsertResult);
        }
    }
}
