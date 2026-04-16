using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Games.Commands.UploadOpponentLogo;

namespace AppTorcedor.Application.Tests;

public sealed class UploadOpponentLogoCommandHandlerTests
{
    [Fact]
    public async Task Handle_returns_null_when_library_returns_null()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        var fake = new FakeLibrary();
        var handler = new UploadOpponentLogoCommandHandler(fake);
        var r = await handler.Handle(new UploadOpponentLogoCommand(stream, "a.png", "image/png"), CancellationToken.None);
        Assert.Null(r);
        Assert.Single(fake.UploadCalls);
    }

    [Fact]
    public async Task Handle_returns_url_when_library_succeeds()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        var fake = new FakeLibrary { Result = new UploadOpponentLogoResult("/uploads/opponent-logos/x.png") };
        var handler = new UploadOpponentLogoCommandHandler(fake);
        var r = await handler.Handle(new UploadOpponentLogoCommand(stream, "a.png", "image/png"), CancellationToken.None);
        Assert.NotNull(r);
        Assert.Equal("/uploads/opponent-logos/x.png", r!.Url);
    }

    private sealed class FakeLibrary : IOpponentLogoLibraryAdminPort
    {
        public List<(Stream Content, string FileName, string ContentType)> UploadCalls { get; } = [];
        public UploadOpponentLogoResult? Result { get; init; }

        public Task<UploadOpponentLogoResult?> UploadAndRegisterAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            UploadCalls.Add((content, fileName, contentType));
            return Task.FromResult(Result);
        }

        public Task<OpponentLogoAssetListPageDto> ListAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new OpponentLogoAssetListPageDto(0, []));
    }
}
