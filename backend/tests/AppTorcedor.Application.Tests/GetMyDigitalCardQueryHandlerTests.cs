using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account.Queries.GetMyDigitalCard;
using Xunit;

namespace AppTorcedor.Application.Tests;

public sealed class GetMyDigitalCardQueryHandlerTests
{
    [Fact]
    public async Task Handler_delegates_to_torcedor_port()
    {
        var uid = Guid.NewGuid();
        var expected = MyDigitalCardViewFactory.NoMembershipRow();
        var fake = new FakeTorcedorPort { Result = expected };
        var handler = new GetMyDigitalCardQueryHandler(fake);
        var r = await handler.Handle(new GetMyDigitalCardQuery(uid), CancellationToken.None);
        Assert.Same(expected, r);
        Assert.Single(fake.Calls);
        Assert.Equal(uid, fake.Calls[0]);
    }

    private sealed class FakeTorcedorPort : IDigitalCardTorcedorPort
    {
        public MyDigitalCardViewDto Result { get; init; } = MyDigitalCardViewFactory.NoMembershipRow();
        public List<Guid> Calls { get; } = [];

        public Task<MyDigitalCardViewDto> GetMyDigitalCardAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            Calls.Add(userId);
            return Task.FromResult(Result);
        }
    }
}
