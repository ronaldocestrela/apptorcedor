using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SocioTorcedor.Modules.Identity.Infrastructure.Entities;
using SocioTorcedor.Modules.Identity.Infrastructure.Persistence;

namespace SocioTorcedor.Modules.Identity.Infrastructure.Tests.Persistence;

public class RoleTenantSeedTests
{
    [Fact]
    public async Task SeedAsync_inserts_Socio_and_Administrador_when_database_has_no_roles()
    {
        var options = new DbContextOptionsBuilder<TenantIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new TenantIdentityDbContext(options);

        await RoleTenantSeed.SeedAsync(db, CancellationToken.None);

        var names = await db.Roles.AsNoTracking()
            .Where(r => r.Name != null)
            .Select(r => r.Name!)
            .ToListAsync();

        names.Should().BeEquivalentTo(new[] { RoleTenantSeed.SocioRoleName, RoleTenantSeed.AdministradorRoleName });
    }

    [Fact]
    public async Task SeedAsync_is_idempotent_when_called_twice()
    {
        var options = new DbContextOptionsBuilder<TenantIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new TenantIdentityDbContext(options);

        await RoleTenantSeed.SeedAsync(db, CancellationToken.None);
        await RoleTenantSeed.SeedAsync(db, CancellationToken.None);

        (await db.Roles.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task SeedAsync_only_adds_roles_that_are_missing()
    {
        var options = new DbContextOptionsBuilder<TenantIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new TenantIdentityDbContext(options);

        db.Roles.Add(new ApplicationRole
        {
            Id = Guid.NewGuid().ToString(),
            Name = RoleTenantSeed.SocioRoleName,
            NormalizedName = RoleTenantSeed.SocioRoleName.ToUpperInvariant(),
            Description = "pre-existing",
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });
        await db.SaveChangesAsync();

        await RoleTenantSeed.SeedAsync(db, CancellationToken.None);

        (await db.Roles.CountAsync()).Should().Be(2);
        (await db.Roles.AnyAsync(r => r.Name == RoleTenantSeed.AdministradorRoleName)).Should().BeTrue();
    }
}
