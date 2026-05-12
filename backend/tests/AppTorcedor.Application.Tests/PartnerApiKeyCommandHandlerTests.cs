using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreatePartnerApiKey;
using AppTorcedor.Application.Modules.Administration.Commands.RevokePartnerApiKey;
using AppTorcedor.Application.Modules.Administration.Queries.ListPartnerApiKeys;
using AppTorcedor.Application.Modules.Partner.Queries.LookupByPhone;
using Xunit;

namespace AppTorcedor.Application.Tests;

public sealed class PartnerApiKeyCommandHandlerTests
{
    private sealed class FakePartnerApiKeyPort : IPartnerApiKeyPort
    {
        public string? LastCreatedName { get; private set; }
        public Guid? LastCreatedBy { get; private set; }
        public Guid? LastRevokedId { get; private set; }
        public bool RevokeResult { get; set; } = true;

        private readonly PartnerApiKeyCreatedDto _createdDto = new(
            Guid.NewGuid(), "Parceiro Teste", "sk_partner_AB", "sk_partner_ABCDE12345XY", DateTimeOffset.UtcNow);

        private readonly List<PartnerApiKeyListItemDto> _list =
        [
            new(Guid.NewGuid(), "Parceiro A", "sk_partner_AA", true, DateTimeOffset.UtcNow, null),
        ];

        public Task<PartnerApiKeyCreatedDto> CreateAsync(string name, Guid? createdByUserId, CancellationToken cancellationToken = default)
        {
            LastCreatedName = name;
            LastCreatedBy = createdByUserId;
            return Task.FromResult(_createdDto);
        }

        public Task<IReadOnlyList<PartnerApiKeyListItemDto>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PartnerApiKeyListItemDto>>(_list);

        public Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            LastRevokedId = id;
            return Task.FromResult(RevokeResult);
        }

        public Task<ValidatedPartnerKeyDto?> ValidateAsync(string rawKey, CancellationToken cancellationToken = default)
            => Task.FromResult<ValidatedPartnerKeyDto?>(null);
    }

    [Fact]
    public async Task Create_command_forwards_name_and_caller_to_port()
    {
        var fake = new FakePartnerApiKeyPort();
        var handler = new CreatePartnerApiKeyCommandHandler(fake);
        var callerId = Guid.NewGuid();

        var result = await handler.Handle(new CreatePartnerApiKeyCommand("Parceiro X", callerId), CancellationToken.None);

        Assert.Equal("Parceiro X", fake.LastCreatedName);
        Assert.Equal(callerId, fake.LastCreatedBy);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Revoke_command_forwards_id_to_port()
    {
        var fake = new FakePartnerApiKeyPort();
        var handler = new RevokePartnerApiKeyCommandHandler(fake);
        var id = Guid.NewGuid();

        var result = await handler.Handle(new RevokePartnerApiKeyCommand(id), CancellationToken.None);

        Assert.Equal(id, fake.LastRevokedId);
        Assert.True(result);
    }

    [Fact]
    public async Task Revoke_command_returns_false_when_port_returns_false()
    {
        var fake = new FakePartnerApiKeyPort { RevokeResult = false };
        var handler = new RevokePartnerApiKeyCommandHandler(fake);

        var result = await handler.Handle(new RevokePartnerApiKeyCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task List_query_handler_returns_port_result()
    {
        var fake = new FakePartnerApiKeyPort();
        var handler = new ListPartnerApiKeysQueryHandler(fake);

        var result = await handler.Handle(new ListPartnerApiKeysQuery(), CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Equal("Parceiro A", result[0].Name);
    }
}

public sealed class LookupPartnerByPhoneQueryHandlerTests
{
    private sealed class FakePartnerLookupPort : IPartnerLookupPort
    {
        public string? LastPhone { get; private set; }
        public PartnerLookupResultDto NextResult { get; set; } = new(true, true);

        public Task<PartnerLookupResultDto> LookupByPhoneAsync(string rawPhone, CancellationToken cancellationToken = default)
        {
            LastPhone = rawPhone;
            return Task.FromResult(NextResult);
        }
    }

    [Fact]
    public async Task Handler_forwards_phone_to_port()
    {
        var fake = new FakePartnerLookupPort();
        var handler = new LookupPartnerByPhoneQueryHandler(fake);

        var result = await handler.Handle(new LookupPartnerByPhoneQuery("11999999999"), CancellationToken.None);

        Assert.Equal("11999999999", fake.LastPhone);
        Assert.True(result.Exists);
        Assert.True(result.IsActiveMember);
    }

    [Fact]
    public async Task Handler_returns_not_found_when_port_returns_false()
    {
        var fake = new FakePartnerLookupPort { NextResult = new(false, false) };
        var handler = new LookupPartnerByPhoneQueryHandler(fake);

        var result = await handler.Handle(new LookupPartnerByPhoneQuery("11000000000"), CancellationToken.None);

        Assert.False(result.Exists);
        Assert.False(result.IsActiveMember);
    }

    [Fact]
    public async Task Handler_returns_exists_but_not_active_member()
    {
        var fake = new FakePartnerLookupPort { NextResult = new(true, false) };
        var handler = new LookupPartnerByPhoneQueryHandler(fake);

        var result = await handler.Handle(new LookupPartnerByPhoneQuery("11999999999"), CancellationToken.None);

        Assert.True(result.Exists);
        Assert.False(result.IsActiveMember);
    }
}
