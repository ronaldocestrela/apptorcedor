using AppTorcedor.Application.Modules.Account;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Services.Account;
using AppTorcedor.Infrastructure.Services.Email;
using AppTorcedor.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AppTorcedor.Infrastructure.Tests;

/// <summary>Validação/unicidade de CPF no <see cref="TorcedorAccountService.UpsertProfileAsync"/> (in-memory) sem subir a API.</summary>
public sealed class TorcedorAccountServiceCpfTests
{
    private static async Task<(AppDbContext Db, TorcedorAccountService Sut, Guid UserA, Guid UserB)> CreateSutWithTwoUsersAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        db.Users.Add(
            new ApplicationUser
            {
                Id = userA,
                UserName = "a@test",
                NormalizedUserName = "A@TEST",
                Email = "a@test",
                NormalizedEmail = "A@TEST",
                EmailConfirmed = true,
                Name = "A",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        db.Users.Add(
            new ApplicationUser
            {
                Id = userB,
                UserName = "b@test",
                NormalizedUserName = "B@TEST",
                Email = "b@test",
                NormalizedEmail = "B@TEST",
                EmailConfirmed = true,
                Name = "B",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        // Apenas o DbContext é usado em UpsertProfile; demais portas do ctor não entram nesses caminhos.
        var sut = new TorcedorAccountService(
            db,
            null!,
            null!,
            null!,
            new NoopEmailSender(),
            new WelcomeEmailComposer(new EmptyAppConfigurationPort(), NullLogger<WelcomeEmailComposer>.Instance),
            NullLogger<TorcedorAccountService>.Instance);
        return (db, sut, userA, userB);
    }

    [Fact]
    public async Task Upsert_saves_canonical_11_digits_for_masked_valid_cpf()
    {
        var (db, sut, userA, _) = await CreateSutWithTwoUsersAsync();
        await using (db)
        {
            var r = await sut.UpsertProfileAsync(
                userA,
                new MyProfileUpsertDto("111.444.777-35", null, null, null),
                CancellationToken.None);
            Assert.True(r.Succeeded);
            var doc = await db.UserProfiles.AsNoTracking().Select(p => p.Document).FirstAsync();
            Assert.Equal("11144477735", doc);
        }
    }

    [Fact]
    public async Task Second_user_cannot_use_same_cpf()
    {
        var (db, sut, userA, userB) = await CreateSutWithTwoUsersAsync();
        await using (db)
        {
            var ok1 = await sut.UpsertProfileAsync(
                userA,
                new MyProfileUpsertDto("39053344705", null, null, null),
                CancellationToken.None);
            Assert.True(ok1.Succeeded);
            var r2 = await sut.UpsertProfileAsync(
                userB,
                new MyProfileUpsertDto("390.533.447-05", null, null, null),
                CancellationToken.None);
            Assert.False(r2.Succeeded);
            Assert.Equal(ProfileUpsertError.DocumentAlreadyInUse, r2.Error);
        }
    }
}
