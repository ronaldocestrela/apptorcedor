using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Games;
using Microsoft.EntityFrameworkCore;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class GameAdministrationServiceOpponentLogoTests
{
    private static async Task<AppDbContext> CreateDbAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    [Fact]
    public async Task CreateGame_fails_validation_when_logo_url_not_in_library()
    {
        await using var db = await CreateDbAsync();
        var sut = new GameAdministrationService(db);
        var dto = new AdminGameWriteDto("Rival", "Camp", DateTimeOffset.UtcNow.AddDays(1), true, "/uploads/opponent-logos/missing.png");
        var r = await sut.CreateGameAsync(dto);
        Assert.False(r.Ok);
        Assert.Equal(GameMutationError.Validation, r.Error);
        Assert.False(await db.Games.AnyAsync());
    }

    [Fact]
    public async Task CreateGame_succeeds_when_logo_url_registered_in_library()
    {
        await using var db = await CreateDbAsync();
        var url = "/uploads/opponent-logos/abc.png";
        db.OpponentLogoAssets.Add(
            new OpponentLogoAssetRecord { Id = Guid.NewGuid(), PublicUrl = url, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var sut = new GameAdministrationService(db);
        var dto = new AdminGameWriteDto("Rival", "Camp", DateTimeOffset.UtcNow.AddDays(1), true, url);
        var r = await sut.CreateGameAsync(dto);
        Assert.True(r.Ok);
        var row = await db.Games.SingleAsync();
        Assert.Equal(url, row.OpponentLogoUrl);
    }

    [Fact]
    public async Task CreateGame_with_null_logo_succeeds()
    {
        await using var db = await CreateDbAsync();
        var sut = new GameAdministrationService(db);
        var dto = new AdminGameWriteDto("Rival", "Camp", DateTimeOffset.UtcNow.AddDays(1), true, null);
        var r = await sut.CreateGameAsync(dto);
        Assert.True(r.Ok);
        var row = await db.Games.SingleAsync();
        Assert.Null(row.OpponentLogoUrl);
    }
}
