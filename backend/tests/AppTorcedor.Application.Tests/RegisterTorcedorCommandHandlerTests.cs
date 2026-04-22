using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using AppTorcedor.Application.Modules.Account.Commands.RegisterTorcedor;

namespace AppTorcedor.Application.Tests;

public sealed class RegisterTorcedorCommandHandlerTests
{
    private sealed class FakeTorcedorAccount : ITorcedorAccountPort
    {
        public RegisterTorcedorRequest? LastRequest { get; private set; }
        public RegisterTorcedorResult Next { get; set; } = RegisterTorcedorResult.Ok(Guid.NewGuid());

        public Task<RegisterTorcedorResult> RegisterAsync(RegisterTorcedorRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(Next);
        }

        public Task<MyProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ProfileUpsertResult> UpsertProfileAsync(Guid userId, MyProfileUpsertDto patch, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> RequiresProfileCompletionAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> RecordInitialConsentsAsync(
            Guid userId,
            IReadOnlyList<Guid> acceptedLegalDocumentVersionIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<RegisterTorcedorResult> RegisterGoogleUserAsync(
            Guid userId,
            string email,
            string name,
            bool emailVerified,
            string googleSubject,
            IReadOnlyList<Guid> acceptedLegalDocumentVersionIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    [Fact]
    public async Task Handler_trims_name_email_and_phone_before_port()
    {
        var fake = new FakeTorcedorAccount();
        var handler = new RegisterTorcedorCommandHandler(fake);
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        await handler.Handle(
            new RegisterTorcedorCommand("  Ana ", "  ana@test.local  ", "Pass123!", "  11  ", new[] { v1, v2 }),
            CancellationToken.None);

        Assert.NotNull(fake.LastRequest);
        Assert.Equal("Ana", fake.LastRequest!.Name);
        Assert.Equal("ana@test.local", fake.LastRequest.Email);
        Assert.Equal("11", fake.LastRequest.PhoneNumber);
    }
}
